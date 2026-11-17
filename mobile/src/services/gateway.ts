import { read, write } from "../db/storage";
import { bairros, categorias, executores } from "../data/seed";
import type {
  Settings,
  CreateInput,
  Photo,
  ImageMeta,
  Status,
} from "../data/types";
import { dataMode, apiUrl, testMode } from "./config";
import { request } from "./http";
import { ServiceError, UNAVAILABLE } from "./errors";
import {
  parseOccurrence,
  parseLookups,
  parseExecutors,
  parseRisk,
  testHeaders,
} from "./contract";
import { createMockEngine, type MockState } from "./mock-engine";
import { cachePhoto, retainPhoto } from "./photos";
const mock = createMockEngine({
  read: () => read<MockState | null>("zelasp-v3-mock-server", null),
  write: (value) => write("zelasp-v3-mock-server", value),
});
async function guard(settings: Settings) {
  await new Promise((r) => setTimeout(r, 300));
  if (settings.offline)
    throw new ServiceError(
      "Sem conexão. Conecte-se para realizar esta operação.",
    );
  if (testMode && settings.fail) throw new ServiceError(UNAVAILABLE, 503);
}
export async function getCatalogs(settings: Settings) {
  await guard(settings);
  if (dataMode === "mock") return { bairros, categorias, executores };
  const [b, c] = await Promise.all([
    request("/api/bairros"),
    request("/api/categorias"),
  ]);
  let actors: typeof executores = [];
  try {
    actors = parseExecutors(await request("/api/equipe"));
  } catch {
    /* Cadastro e consulta continuam; operações com executor ficam desabilitadas. */
  }
  return {
    bairros: parseLookups(b),
    categorias: parseLookups(c),
    executores: actors,
  };
}
export async function getOccurrences(settings: Settings) {
  await guard(settings);
  if (testMode && settings.empty) return [];
  if (dataMode === "mock") return mock.list();
  const response = await request<unknown>("/api/ocorrencias");
  if (!Array.isArray(response))
    throw new ServiceError(
      "A API retornou uma lista fora do formato combinado.",
    );
  return response.map(parseOccurrence);
}
export async function getDetail(id: number, settings: Settings) {
  await guard(settings);
  return dataMode === "mock"
    ? mock.detail(id)
    : parseOccurrence(await request(`/api/ocorrencias/${id}`));
}
export async function createOccurrence(input: CreateInput, settings: Settings) {
  await guard(settings);
  if (dataMode === "mock") return mock.create(input, settings);
  const value = await request(
    "/api/ocorrencias",
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...testHeaders(settings, testMode, "create"),
      },
      body: JSON.stringify(input),
    },
    true,
  );
  try {
    return parseOccurrence(value);
  } catch {
    throw new ServiceError(
      "O registro pode ter sido criado, mas sua resposta não foi confirmada. Confira a lista antes de repetir.",
      0,
      true,
    );
  }
}
export async function uploadPhoto(
  id: number,
  photo: Photo,
  titulo: string,
  executor: string,
  settings: Settings,
): Promise<ImageMeta> {
  await guard(settings);
  if (dataMode === "mock") {
    const meta = await mock.upload(id, photo, titulo, executor);
    await mock.replacePhoto(meta.id, await retainPhoto(photo, meta.id));
    return meta;
  }
  const form = new FormData();
  if (photo.uri.startsWith("data:") || photo.uri.startsWith("blob:"))
    form.append("arquivo", await (await fetch(photo.uri)).blob(), photo.nome);
  else
    form.append("arquivo", {
      uri: photo.uri,
      name: photo.nome,
      type: photo.mime,
    } as unknown as Blob);
  form.append("titulo", titulo);
  form.append("ocorrenciaId", String(id));
  form.append("executor", executor);
  const response = await request<ImageMeta>(
    "/api/imagens",
    { method: "POST", body: form },
    true,
  );
  if (!response || !Number.isInteger(response.id))
    throw new ServiceError(
      "Não foi possível confirmar o anexo. Consulte o detalhe antes de reenviar.",
      0,
      true,
    );
  return {
    ...response,
    ocorrenciaId: id,
    titulo: response.titulo ?? titulo,
    executor: response.executor ?? executor,
    mime: response.mime ?? photo.mime,
    nomeArquivo: response.nomeArquivo ?? photo.nome,
    dataUtc: response.dataUtc ?? new Date().toISOString(),
  };
}
export async function getPhoto(id: number, settings: Settings) {
  await guard(settings);
  if (dataMode === "mock") return mock.photo(id);
  return cachePhoto(`${apiUrl}/api/imagens/${id}/conteudo`, id);
}
export async function changeStatus(
  id: number,
  status: Status,
  executor: string,
  settings: Settings,
) {
  await guard(settings);
  if (dataMode === "mock")
    return mock.changeStatus(id, status, executor, settings);
  await request(
    `/api/ocorrencias/${id}/status`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...testHeaders(settings, testMode, "status"),
      },
      body: JSON.stringify({ status, executor }),
    },
    true,
  );
  try {
    return await getDetail(id, settings);
  } catch {
    throw new ServiceError(
      "A alteração foi enviada, mas não foi possível confirmar o detalhe. Atualize antes de repetir.",
      0,
      true,
    );
  }
}
export async function getRisk(settings: Settings) {
  await guard(settings);
  return dataMode === "mock"
    ? mock.risk(settings.emptyRisk)
    : parseRisk(await request("/api/risco-cidadao"));
}
export async function resetMock() {
  if (dataMode !== "mock")
    throw new ServiceError(
      "A restauração só está disponível no modo simulado.",
    );
  await mock.reset();
}
