import { makeSeed, bairros, categorias, executores } from "../data/seed";
import { demoPhoto } from "../data/demo-photo";
import type {
  CreateInput,
  Occurrence,
  Photo,
  Settings,
  SecurityEvent,
  Status,
  ImageMeta,
} from "../data/types";
import { maskDescription, riskFromEvents } from "./privacy";
import { validateDescription, validatePhoto } from "../utils/validation";
import { ServiceError } from "./errors";
export interface MockState {
  rows: Occurrence[];
  events: SecurityEvent[];
  images: Record<string, Photo>;
}
export interface MockStore {
  read(): Promise<MockState | null>;
  write(state: MockState): Promise<void>;
}
export function createMockEngine(store: MockStore, clock = () => new Date()) {
  let tail = Promise.resolve();
  const serial = <T>(fn: () => Promise<T>): Promise<T> => {
    const result = tail.then(fn);
    tail = result.then(
      () => {},
      () => {},
    );
    return result;
  };
  async function state(): Promise<MockState> {
    const saved = await store.read();
    if (saved) return saved;
    const initial = { ...makeSeed(clock()), images: { "1": demoPhoto } };
    await store.write(initial);
    return initial;
  }
  const clone = <T>(x: T): T => JSON.parse(JSON.stringify(x));
  function executor(id: string) {
    if (!executores.some((x) => x.id === id))
      throw new ServiceError("Executor não cadastrado.", 400);
  }
  return {
    list: async () => clone((await state()).rows),
    detail: async (id: number) => {
      const row = (await state()).rows.find((x) => x.id === id);
      if (!row) throw new ServiceError("Ocorrência não encontrada.", 404);
      return clone(row);
    },
    photo: async (id: number) => clone((await state()).images[id] ?? null),
    create: (input: CreateInput, flags: Settings) =>
      serial(async () => {
        const bad = validateDescription(input.descricao);
        if (bad) throw new ServiceError(bad, 400);
        const masked = maskDescription(input.descricao.trim());
        if (
          !bairros.some((x) => x.id === input.bairroId) ||
          !categorias.some((x) => x.id === input.categoriaId)
        ) {
          if (flags.trace)
            for (const type of masked.detections)
              console.info(
                `[TAINT] tipo=${type} origem=descricao destino=ERRO acao=MASCARADO`,
              );
          throw new ServiceError("Bairro ou categoria não encontrado.", 404);
        }
        const current = clone(await state());
        const row: Occurrence = {
          id: Math.max(0, ...current.rows.map((x) => x.id)) + 1,
          bairroId: input.bairroId,
          categoriaId: input.categoriaId,
          descricao: masked.text,
          status: "ABERTA",
          origem: "APP",
          criadaEm: clock().toISOString(),
          historico: [],
          imagens: [],
          dadosMascarados: masked.detections.length > 0,
        };
        let eventId = Math.max(0, ...current.events.map((x) => x.id));
        for (const tipoDado of masked.detections) {
          current.events.push({
            id: ++eventId,
            tipoEvento: "PII_DETECTADO",
            tipoDado,
            destino: "BANCO",
            ocorridoEm: clock().toISOString(),
          });
          if (flags.unsafeLog)
            current.events.push({
              id: ++eventId,
              tipoEvento: "VAZAMENTO_BLOQUEADO",
              tipoDado,
              destino: "LOG",
              ocorridoEm: clock().toISOString(),
            });
          if (flags.trace)
            for (const destino of ["BANCO", "RESPOSTA", "LOG"])
              console.info(
                `[TAINT] tipo=${tipoDado} origem=descricao destino=${destino} acao=MASCARADO`,
              );
        }
        current.rows.push(row);
        await store.write(current);
        return clone(row);
      }),
    upload: (id: number, photo: Photo, titulo: string, who: string) =>
      serial(async () => {
        executor(who);
        const error = validatePhoto(photo.mime, photo.tamanho);
        if (error)
          throw new ServiceError(
            error,
            photo.tamanho > 2 * 1024 * 1024 ? 413 : 400,
          );
        const current = clone(await state());
        const row = current.rows.find((x) => x.id === id);
        if (!row) throw new ServiceError("Ocorrência não encontrada.", 404);
        const imageId =
          Math.max(0, ...Object.keys(current.images).map(Number)) + 1;
        const meta: ImageMeta = {
          id: imageId,
          ocorrenciaId: id,
          titulo: maskDescription(titulo.trim() || "Foto da ocorrência").text,
          nomeArquivo: `foto-${imageId}.${photo.mime === "image/png" ? "png" : "jpg"}`,
          mime: photo.mime,
          dataUtc: clock().toISOString(),
          executor: who,
        };
        row.imagens = [...(row.imagens ?? []), meta];
        current.images[String(imageId)] = { ...photo, nome: meta.nomeArquivo };
        await store.write(current);
        return meta;
      }),
    replacePhoto: (id: number, photo: Photo) =>
      serial(async () => {
        const current = clone(await state());
        current.images[String(id)] = photo;
        await store.write(current);
      }),
    changeStatus: (id: number, status: Status, who: string, flags: Settings) =>
      serial(async () => {
        executor(who);
        if (!["ABERTA", "EM_ANALISE", "RESOLVIDA"].includes(status))
          throw new ServiceError("Status inválido.", 400);
        const current = clone(await state());
        const row = current.rows.find((x) => x.id === id);
        if (!row) throw new ServiceError("Ocorrência não encontrada.", 404);
        if (row.status === status)
          throw new ServiceError(
            "Selecione um status diferente do atual.",
            400,
          );
        row.historico.push({
          statusAnterior: row.status,
          statusNovo: status,
          alteradoEm: clock().toISOString(),
          executor: who,
        });
        row.status = status;
        if (flags.rollback)
          throw new ServiceError(
            "Não foi possível alterar o status. Nenhuma mudança foi gravada.",
            500,
          );
        await store.write(current);
        return clone(row);
      }),
    risk: async (empty = false) =>
      riskFromEvents(empty ? [] : (await state()).events, clock()),
    reset: () =>
      serial(async () => {
        await store.write({ ...makeSeed(clock()), images: { "1": demoPhoto } });
      }),
  };
}
