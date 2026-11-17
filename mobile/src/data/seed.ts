import type { Lookup, Executor, Occurrence, SecurityEvent } from "./types";
export const bairros: Lookup[] = [
  { id: 1, nome: "Bairro Teste Centro" },
  { id: 2, nome: "Bairro Teste Norte" },
];
export const categorias: Lookup[] = [
  { id: 1, nome: "Lixeira cheia" },
  { id: 2, nome: "Descarte irregular" },
  { id: 3, nome: "Buraco" },
  { id: 4, nome: "Iluminação" },
];
export const executores: Executor[] = [
  {
    id: "Djalma_RM555530",
    nome: "Djalma Moreira de Andrade Filho",
    rm: "555530",
  },
  {
    id: "Felipe_RM558447",
    nome: "Felipe Paes de Barros Muller Carioba",
    rm: "558447",
  },
  { id: "Lucas_RM556323", nome: "Lucas Rodrigues de Queiroz", rm: "556323" },
  { id: "VictorHugo_RM554787", nome: "Victor Hugo de Paula", rm: "554787" },
];
export function makeSeed(now = new Date()): {
  rows: Occurrence[];
  events: SecurityEvent[];
} {
  const ago = (days: number) =>
    new Date(now.getTime() - days * 86400000).toISOString();
  return {
    rows: [
      {
        id: 1,
        bairroId: 1,
        categoriaId: 1,
        descricao: "Lixeira cheia próxima à praça.",
        status: "ABERTA",
        origem: "APP",
        criadaEm: ago(1),
        historico: [],
        imagens: [],
      },
      {
        id: 2,
        bairroId: 1,
        categoriaId: 2,
        descricao: "Descarte de entulho na calçada",
        status: "EM_ANALISE",
        origem: "APP",
        criadaEm: ago(2),
        historico: [
          {
            statusAnterior: "ABERTA",
            statusNovo: "EM_ANALISE",
            alteradoEm: ago(1),
            executor: "VictorHugo_RM554787",
          },
        ],
        imagens: [
          {
            id: 1,
            ocorrenciaId: 2,
            titulo: "Imagem fictícia de teste",
            nomeArquivo: "foto-teste.jpg",
            mime: "image/jpeg",
            dataUtc: ago(2),
            executor: "VictorHugo_RM554787",
          },
        ],
      },
      {
        id: 3,
        bairroId: 2,
        categoriaId: 1,
        descricao: "Lixeira cheia no ponto de coleta.",
        status: "RESOLVIDA",
        origem: "APP",
        criadaEm: ago(3),
        historico: [
          {
            statusAnterior: "ABERTA",
            statusNovo: "EM_ANALISE",
            alteradoEm: ago(2),
            executor: "VictorHugo_RM554787",
          },
          {
            statusAnterior: "EM_ANALISE",
            statusNovo: "RESOLVIDA",
            alteradoEm: ago(1),
            executor: "VictorHugo_RM554787",
          },
        ],
        imagens: [],
      },
    ],
    events: (["CPF", "CPF", "TELEFONE", "SENHA"] as const).map(
      (tipoDado, i) => ({
        id: i + 1,
        tipoEvento: "PII_DETECTADO",
        tipoDado,
        destino: "BANCO",
        ocorridoEm: ago(1),
      }),
    ),
  };
}
