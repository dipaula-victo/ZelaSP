import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useRef,
  useCallback,
} from "react";
import NetInfo from "@react-native-community/netinfo";
import { read, write } from "../db/storage";
import { dataMode, testMode } from "../services/config";
import * as api from "../services/gateway";
import { retainPhoto } from "../services/photos";
import { ServiceError } from "../services/errors";
import { createRiskCoordinator, restoreRiskCache } from "../services/risk-state";
import { parseCachedRisk } from "../services/contract";
import {
  defaults,
  emptySnapshot,
  type Snapshot,
  type Settings,
  type CreateInput,
  type Photo,
  type Status,
  type Occurrence,
} from "../data/types";
const key = `zelasp-v3:${dataMode}:snapshot`;
function useStore() {
  const [snapshot, setSnapshot] = useState<Snapshot>(emptySnapshot),
    [settings, setConfig] = useState(defaults),
    [ready, setReady] = useState(false),
    [fatal, setFatal] = useState(""),
    [loading, setLoading] = useState(false),
    [busy, setBusy] = useState(false),
    [error, setError] = useState(""),
    [stale, setStale] = useState(false),
    [networkOffline, setNetworkOffline] = useState(false);
  const saved = useRef(snapshot),
    config = useRef(settings),
    network = useRef(false),
    lock = useRef(false),
    pending = useRef(Promise.resolve());
  const riskCoordinator = useRef(createRiskCoordinator());
  const persist = useCallback(async (patch: Partial<Snapshot>) => {
    const merged = { ...saved.current, ...patch };
    const next = { ...merged, risk: parseCachedRisk(merged.risk) };
    saved.current = next;
    setSnapshot(next);
    const task = pending.current.catch(() => {}).then(() => write(key, next));
    pending.current = task;
    await task;
  }, []);
  const flags = () => ({
    ...config.current,
    offline: config.current.offline || network.current,
  });
  const remember = async (row: Occurrence) =>
    persist({
      rows: [row, ...saved.current.rows.filter((x) => x.id !== row.id)],
    });
  const refresh = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const current = flags();
      const [rows, catalogs] = await Promise.all([
        api.getOccurrences(current),
        api.getCatalogs(current),
      ]);
      await persist({
        ...catalogs,
        rows: rows.map((row) => ({
          ...saved.current.rows.find((x) => x.id === row.id),
          ...row,
        })),
        updatedAt: new Date().toISOString(),
      });
      setStale(false);
    } catch (e) {
      setStale(true);
      setError(e instanceof Error ? e.message : "Não foi possível atualizar.");
    } finally {
      setLoading(false);
    }
  }, [persist]);
  useEffect(
    () =>
      NetInfo.addEventListener((state) => {
        const disconnected =
          state.isConnected === false || state.isInternetReachable === false;
        network.current = disconnected;
        setNetworkOffline(disconnected);
      }),
    [],
  );
  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const [cache, options] = await Promise.all([
          restoreRiskCache(() => read(key, emptySnapshot), value => write(key, value)),
          read("zelasp-v3:settings", defaults),
        ]);
        if (!active) return;
        saved.current = cache;
        setSnapshot(cache);
        config.current = testMode ? { ...defaults, ...options } : defaults;
        setConfig(config.current);
        setReady(true);
        await refresh();
      } catch (e) {
        if (active)
          setFatal("Falha ao abrir ou proteger o armazenamento local.");
      }
    })();
    return () => {
      active = false;
      riskCoordinator.current.invalidate();
    };
  }, [refresh]);
  async function mutate<T>(work: () => Promise<T>) {
    if (flags().offline)
      throw new ServiceError(
        "Sem conexão. Apenas a consulta dos dados salvos está disponível.",
      );
    if (lock.current)
      throw new ServiceError("Aguarde a operação em andamento.");
    lock.current = true;
    setBusy(true);
    try {
      return await work();
    } finally {
      lock.current = false;
      setBusy(false);
    }
  }
  return {
    snapshot,
    settings,
    ready,
    fatal,
    loading,
    busy,
    error,
    stale,
    offline: settings.offline || networkOffline,
    refresh,
    setSettings: async (value: Settings) => {
      await write("zelasp-v3:settings", value);
      riskCoordinator.current.invalidate();
      config.current = value;
      setConfig(value);
      await refresh();
    },
    create: (input: CreateInput) =>
      mutate(async () => {
        const row = await api.createOccurrence(input, flags());
        try {
          await remember(row);
          await persist({ myIds: [...saved.current.myIds, row.id] });
        } catch {
          throw new ServiceError(
            `Protocolo #ZSP-${row.id} criado. Falha ao salvar a cópia local; consulte a lista antes de repetir.`,
            0,
            true,
          );
        }
        return row;
      }),
    detail: async (id: number) => {
      try {
        const row = await api.getDetail(id, flags());
        await remember(row);
        return { row, cached: false };
      } catch (e) {
        if (e instanceof ServiceError && e.status === 404) throw e;
        const row = saved.current.rows.find((x) => x.id === id);
        if (row) return { row, cached: true };
        throw e;
      }
    },
    changeStatus: (id: number, status: Status, executor: string) =>
      mutate(async () => {
        const row = await api.changeStatus(id, status, executor, flags());
        await remember(row);
        return row;
      }),
    attachPhoto: (id: number, photo: Photo, title: string, executor: string) =>
      mutate(async () => {
        const meta = await api.uploadPhoto(id, photo, title, executor, flags());
        try {
          const permanent =
            dataMode === "mock"
              ? await api.getPhoto(meta.id, flags())
              : await retainPhoto(photo, meta.id);
          if (!permanent)
            throw new ServiceError(
              "Foto enviada, mas não foi possível salvar a cópia local. Atualize o detalhe antes de reenviar.",
              0,
              true,
            );
          await persist({
            photos: { ...saved.current.photos, [meta.id]: permanent },
          });
          const row = await api.getDetail(id, flags());
          await remember(row);
          return row;
        } catch {
          throw new ServiceError(
            "Foto enviada. Atualize o detalhe para confirmar o anexo antes de reenviar.",
            0,
            true,
          );
        }
      }),
    photo: async (id: number) => {
      if (saved.current.photos[id]) return saved.current.photos[id];
      const photo = await api.getPhoto(id, flags());
      if (photo)
        await persist({ photos: { ...saved.current.photos, [id]: photo } });
      return photo;
    },
    cancelRisk: () => riskCoordinator.current.invalidate(),
    refreshRisk: () => riskCoordinator.current.refresh(
      () => `${dataMode}:${JSON.stringify(flags())}`,
      () => api.getRisk(flags()),
      risk => persist({ risk, riskUpdatedAt: new Date().toISOString() }),
    ),
    reset: async () => {
      riskCoordinator.current.invalidate();
      await api.resetMock();
      await persist(emptySnapshot);
      await refresh();
    },
  };
}
const Context = createContext<ReturnType<typeof useStore> | null>(null);
export function AppProvider({ children }: { children: React.ReactNode }) {
  return <Context.Provider value={useStore()}>{children}</Context.Provider>;
}
export function useApp() {
  const value = useContext(Context);
  if (!value) throw new Error("AppProvider ausente");
  return value;
}
