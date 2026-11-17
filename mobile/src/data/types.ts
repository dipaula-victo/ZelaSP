export type Status = "ABERTA" | "EM_ANALISE" | "RESOLVIDA";
export interface Lookup {
  id: number;
  nome: string;
}
export interface Executor {
  id: string;
  nome: string;
  rm?: string;
}
export interface History {
  statusAnterior: Status;
  statusNovo: Status;
  alteradoEm: string;
  executor: string;
}
export interface ImageMeta {
  id: number;
  ocorrenciaId: number;
  titulo: string;
  nomeArquivo: string;
  mime: string;
  dataUtc: string;
  executor: string;
}
export interface Occurrence {
  id: number;
  bairroId: number;
  categoriaId: number;
  descricao: string;
  status: Status;
  origem: "APP" | "SENSOR";
  criadaEm: string;
  historico: History[];
  imagens?: ImageMeta[];
  imagemId?: number;
  dadosMascarados?: boolean;
}
export interface Photo {
  uri: string;
  nome: string;
  mime: "image/jpeg" | "image/png";
  tamanho: number;
}
export interface Settings {
  offline: boolean;
  fail: boolean;
  empty: boolean;
  rollback: boolean;
  unsafeLog: boolean;
  trace: boolean;
  emptyRisk: boolean;
}
export type PiiType = "CPF" | "TELEFONE" | "EMAIL" | "SENHA" | "SAUDE";
export interface SecurityEvent {
  id: number;
  tipoEvento: "PII_DETECTADO" | "VAZAMENTO_BLOQUEADO";
  tipoDado: PiiType;
  destino: "BANCO" | "LOG";
  ocorridoEm: string;
}
export interface Risk {
  pontuacao: number;
  nivel: "BAIXO" | "MEDIO" | "ALTO";
  periodoDias: 7;
  deteccoes: {
    cpf: number;
    telefone: number;
    email: number;
    senha: number;
    saude: number;
  };
  vazamentosBloqueados: number;
  alertas: { tipo: string; texto: string }[];
}
export interface CreateInput {
  bairroId: number;
  categoriaId: number;
  descricao: string;
  origem: "APP";
}
export interface Snapshot {
  rows: Occurrence[];
  bairros: Lookup[];
  categorias: Lookup[];
  executores: Executor[];
  photos: Record<string, Photo>;
  myIds: number[];
  updatedAt: string | null;
  risk: Risk | null;
  riskUpdatedAt: string | null;
}
export const statusLabel: Record<Status, string> = {
  ABERTA: "Aberta",
  EM_ANALISE: "Em análise",
  RESOLVIDA: "Resolvida",
};
export const defaults: Settings = {
  offline: false,
  fail: false,
  empty: false,
  rollback: false,
  unsafeLog: false,
  trace: false,
  emptyRisk: false,
};
export const emptySnapshot: Snapshot = {
  rows: [],
  bairros: [],
  categorias: [],
  executores: [],
  photos: {},
  myIds: [],
  updatedAt: null,
  risk: null,
  riskUpdatedAt: null,
};
