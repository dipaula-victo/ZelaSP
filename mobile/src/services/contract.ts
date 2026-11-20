import type {
  Occurrence,
  Risk,
  Lookup,
  Executor,
  Settings,
  Status,
} from "../data/types";
import { ServiceError } from "./errors";
import { maskDescription, alerts } from "./privacy";
const validStatus = (s: unknown): s is Status =>
  ["ABERTA", "EM_ANALISE", "RESOLVIDA"].includes(String(s));
export function parseOccurrence(value: unknown): Occurrence {
  const x = value as Occurrence;
  if (
    !x ||
    !Number.isInteger(x.id) ||
    x.id <= 0 ||
    typeof x.descricao !== "string" ||
    !validStatus(x.status) ||
    !Number.isInteger(x.bairroId) ||
    !Number.isInteger(x.categoriaId) ||
    typeof x.criadaEm !== "string" ||
    !Number.isFinite(Date.parse(x.criadaEm)) ||
    !["APP", "SENSOR"].includes(x.origem) ||
    !Array.isArray(x.historico)
  )
    throw new ServiceError(
      "A resposta da ocorrência está fora do formato combinado.",
    );
  if (maskDescription(x.descricao).detections.length)
    throw new ServiceError(
      "A resposta contém dados pessoais sem tratamento. Ela não foi exibida nem salva.",
    );
  for (const h of x.historico) {
    if (
      !validStatus(h.statusAnterior) ||
      !validStatus(h.statusNovo) ||
      !Number.isFinite(Date.parse(h.alteradoEm)) ||
      typeof h.executor !== "string"
    )
      throw new ServiceError("O histórico recebido está incompleto.");
  }
  return {
    id: x.id,
    bairroId: x.bairroId,
    categoriaId: x.categoriaId,
    descricao: x.descricao,
    status: x.status,
    origem: x.origem,
    criadaEm: x.criadaEm,
    historico: x.historico.map((h) => ({
      statusAnterior: h.statusAnterior,
      statusNovo: h.statusNovo,
      alteradoEm: h.alteradoEm,
      executor: h.executor,
    })),
    imagens: x.imagens?.map((m) => ({
      id: m.id,
      ocorrenciaId: m.ocorrenciaId,
      titulo: maskDescription(m.titulo ?? "Foto da ocorrência").text,
      nomeArquivo: `foto-${m.id}`,
      mime: m.mime,
      dataUtc: m.dataUtc,
      executor: m.executor,
    })),
    imagemId: x.imagemId,
    dadosMascarados: x.dadosMascarados,
  };
}
export function parseLookups(value: unknown): Lookup[] {
  if (
    !Array.isArray(value) ||
    value.some((x) => !Number.isInteger(x.id) || typeof x.nome !== "string")
  )
    throw new ServiceError("Não foi possível carregar as opções do cadastro.");
  return value;
}
export function parseExecutors(value: unknown): Executor[] {
  const raw = value as { executores?: unknown[]; integrantes?: unknown[] };
  const list = raw?.executores ?? raw?.integrantes;
  if (!Array.isArray(list))
    throw new ServiceError(
      "A API precisa informar os executores cadastrados em /api/equipe.",
    );
  const result = list.map((x: any) =>
    typeof x === "string"
      ? { id: x, nome: x }
      : {
          id: x.executor ?? x.id,
          nome: x.nome ?? x.executor ?? x.id,
          rm: x.rm,
        },
  );
  if (
    result.some(
      (x) => typeof x.id !== "string" || !x.id || typeof x.nome !== "string",
    )
  )
    throw new ServiceError("A lista de executores da API está incompleta.");
  return result;
}
function utcInstant(value: unknown): value is string {
  return typeof value === "string" &&
    /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?Z$/.test(value) &&
    Number.isFinite(Date.parse(value)) &&
    new Date(value).toISOString().slice(0, 19) === value.slice(0, 19);
}

export function parseRisk(value: unknown, requireBackendFields = false): Risk {
  const x = value as Risk;
  const keys = ["cpf", "telefone", "email", "senha", "saude"] as const;
  if (
    !x ||
    !Number.isSafeInteger(x.pontuacao) ||
    x.pontuacao < 0 ||
    x.pontuacao > 100 ||
    x.periodoDias !== 7 ||
    !x.deteccoes ||
    keys.some((k) => !Number.isSafeInteger(x.deteccoes[k]) || x.deteccoes[k] < 0) ||
    !Number.isSafeInteger(x.vazamentosBloqueados) ||
    x.vazamentosBloqueados < 0 ||
    !Array.isArray(x.alertas) || x.alertas.length > 6
  )
    throw new ServiceError(
      "O indicador recebido está fora do formato combinado.",
    );
  const score = Math.min(
    100,
    Math.min(100, x.deteccoes.cpf) * 6 +
      Math.min(100, x.deteccoes.telefone) * 3 +
      Math.min(100, x.deteccoes.email) * 3 +
      Math.min(100, x.deteccoes.senha) * 10 +
      Math.min(100, x.deteccoes.saude) * 8 +
      Math.min(100, x.vazamentosBloqueados) * 15,
  );
  if (
    score !== x.pontuacao ||
    x.nivel !== (score < 20 ? "BAIXO" : score < 50 ? "MEDIO" : "ALTO")
  )
    throw new ServiceError(
      "A pontuação do indicador não corresponde às regras do grupo.",
    );
  for (const a of x.alertas) {
    if (
      !a || typeof a.tipo !== "string" || !Object.prototype.hasOwnProperty.call(alerts, a.tipo) ||
      a.texto !== alerts[a.tipo as keyof typeof alerts]
    )
      throw new ServiceError(
        "O painel recebeu um alerta fora dos textos fixos combinados.",
      );
  }
  const expectedAlerts = [
    x.deteccoes.cpf > 0 ? "CPF" : null,
    x.deteccoes.telefone > 0 ? "TELEFONE" : null,
    x.deteccoes.email > 0 ? "EMAIL" : null,
    x.deteccoes.senha > 0 ? "SENHA" : null,
    x.deteccoes.saude > 0 ? "SAUDE" : null,
    x.vazamentosBloqueados > 0 ? "VAZAMENTO" : null,
  ].filter(Boolean);
  if (x.alertas.length !== expectedAlerts.length ||
      new Set(x.alertas.map(a => a.tipo)).size !== x.alertas.length ||
      x.alertas.some(a => !expectedAlerts.includes(a.tipo)))
    throw new ServiceError("Os alertas não correspondem aos eventos agregados.");

  let janela: Risk["janela"];
  if (requireBackendFields || x.janela !== undefined) {
    if (!x.janela || !utcInstant(x.janela.inicioUtc) || !utcInstant(x.janela.fimUtc) ||
        Date.parse(x.janela.fimUtc) - Date.parse(x.janela.inicioUtc) !== 7 * 86400000)
      throw new ServiceError("A janela de avaliação recebida é inválida.");
    janela = { inicioUtc: x.janela.inicioUtc, fimUtc: x.janela.fimUtc };
  }

  let explicacao: Risk["explicacao"];
  if (requireBackendFields || x.explicacao !== undefined) {
    const e = x.explicacao;
    if (!e || !["LLM", "FALLBACK"].includes(e.origem) ||
        typeof e.texto !== "string" || !e.texto.trim() || e.texto !== e.texto.trim() ||
        e.texto.length > 600 || e.texto.toLowerCase().includes("http") ||
        Array.from(e.texto).some(c => /[\p{Cc}\p{Cf}]/u.test(c) || "<>[]{}\\/@".includes(c)) ||
        maskDescription(e.texto.normalize("NFC")).detections.length > 0)
      throw new ServiceError("A explicação recebida não passou pela validação de segurança.");
    explicacao = { texto: e.texto, origem: e.origem };
  }

  // Rebuild an allowlisted response: free fields/unknown categories never reach cache or UI.
  return {
    pontuacao: x.pontuacao, nivel: x.nivel, periodoDias: 7,
    deteccoes: { cpf: x.deteccoes.cpf, telefone: x.deteccoes.telefone,
      email: x.deteccoes.email, senha: x.deteccoes.senha, saude: x.deteccoes.saude },
    vazamentosBloqueados: x.vazamentosBloqueados,
    alertas: x.alertas.map(a => ({ tipo: a.tipo, texto: alerts[a.tipo as keyof typeof alerts] })),
    ...(janela ? { janela } : {}), ...(explicacao ? { explicacao } : {}),
  };
}

export function parseCachedRisk(value: unknown): Risk | null {
  if (value == null) return null;
  try { return parseRisk(value); } catch { return null; }
}
export function testHeaders(
  settings: Settings,
  enabled: boolean,
  operation: "create" | "status",
) {
  if (!enabled) return {};
  const headers: Record<string, string> = {};
  if (settings.trace) headers["X-Rastrear-Fluxo"] = "true";
  if (operation === "create" && settings.unsafeLog)
    headers["X-Simular-Log-Inseguro"] = "true";
  if (operation === "status" && settings.rollback)
    headers["X-Simular-Falha"] = "apos-escritas";
  return headers;
}
