import test from "node:test";
import assert from "node:assert/strict";
import { DatabaseSync } from "node:sqlite";
import { mkdtempSync, rmSync, readFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import {
  maskDescription,
  validCPF,
  riskFromEvents,
} from "../src/services/privacy";
import { createMockEngine, type MockState } from "../src/services/mock-engine";
import {
  parseOccurrence,
  parseRisk,
  testHeaders,
  parseExecutors,
} from "../src/services/contract";
import { defaults, type SecurityEvent } from "../src/data/types";
import { makeSeed } from "../src/data/seed";
import {
  validateDescription,
  validateImageBytes,
  MAX_PHOTO_BYTES,
} from "../src/utils/validation";
import { schema, upsert } from "../src/db/schema";
import { demoPhoto } from "../src/data/demo-photo";
const now = new Date("2026-11-14T12:00:00Z");
function harness() {
  let data: MockState | null = null;
  return {
    engine: createMockEngine(
      {
        read: async () => data,
        write: async (value) => {
          data = structuredClone(value);
        },
      },
      () => now,
    ),
    get: () => data,
  };
}
test("CPF: dígitos verificadores, pontuação, repetidos e inválido", () => {
  assert.ok(validCPF("111.444.777-35"));
  assert.ok(validCPF("11144477735"));
  assert.equal(validCPF("12345678900"), false);
  assert.equal(validCPF("11111111111"), false);
});
for (const [input, expected] of [
  ["CPF 11144477735", "CPF [CPF OCULTADO]"],
  ["(11) 99999-0000", "[TELEFONE OCULTADO]"],
  ["11999990000", "[TELEFONE OCULTADO]"],
  ["teste@example.com", "[E-MAIL OCULTADO]"],
  ["senha: ficticia", "senha: [SENHA OCULTADA]"],
  ["senha=ficticia", "senha=[SENHA OCULTADA]"],
  ["senha é ficticia", "senha é [SENHA OCULTADA]"],
  [
    "diabetes hipertensão câncer HIV depressão epilepsia",
    "[SAÚDE OCULTADA] [SAÚDE OCULTADA] [SAÚDE OCULTADA] [SAÚDE OCULTADA] [SAÚDE OCULTADA] [SAÚDE OCULTADA]",
  ],
])
  test(`Mascara ${expected}`, () => {
    assert.equal(maskDescription(input).text, expected);
    assert.deepEqual(maskDescription(expected).detections, []);
  });
test("Endereço e CPF inválido permanecem sem eventos", () => {
  const s = "Rua Exemplo, 100. CPF 12345678900";
  assert.deepEqual(maskDescription(s), { text: s, detections: [] });
});
test("Seed contém três casos, histórias 0/1/2 e risco 25 médio", () => {
  const seed = makeSeed(now);
  assert.deepEqual(
    seed.rows.map((x) => x.historico.length),
    [0, 1, 2],
  );
  const risk = riskFromEvents(seed.events, now);
  assert.equal(risk.pontuacao, 25);
  assert.equal(risk.nivel, "MEDIO");
  assert.equal(risk.deteccoes.cpf, 2);
  assert.deepEqual(parseRisk(risk), risk);
});
test("Janela de sete dias exclui antigos e futuros", () => {
  const events = makeSeed(now).events;
  for (const e of events) e.ocorridoEm = "2026-11-01T00:00:00Z";
  assert.equal(riskFromEvents(events, now).pontuacao, 0);
  events[0].ocorridoEm = "2026-11-15T00:00:00Z";
  assert.equal(riskFromEvents(events, now).pontuacao, 0);
});
for (const [score, types, level] of [
  [19, ["SENHA", "CPF", "TELEFONE"], "BAIXO"],
  [20, ["SENHA", "SENHA"], "MEDIO"],
  [49, ["SENHA", "SENHA", "SENHA", "SENHA", "CPF", "TELEFONE"], "MEDIO"],
  [50, ["SENHA", "SENHA", "SENHA", "SENHA", "SENHA"], "ALTO"],
] as const)
  test(`Limite de risco ${score}`, () => {
    const events = types.map((tipoDado, i) => ({
      id: i,
      tipoEvento: "PII_DETECTADO",
      tipoDado,
      destino: "BANCO",
      ocorridoEm: now.toISOString(),
    })) as SecurityEvent[];
    const risk = riskFromEvents(events, now);
    assert.equal(risk.pontuacao, score);
    assert.equal(risk.nivel, level);
  });
test("Cria sem foto; mascara antes de persistir; aumenta risco para 31", async () => {
  const h = harness();
  const row = await h.engine.create(
    {
      bairroId: 1,
      categoriaId: 1,
      descricao: "CPF 11144477735 na Rua Exemplo",
      origem: "APP",
    },
    defaults,
  );
  assert.equal(row.id, 4);
  assert.equal(row.status, "ABERTA");
  assert.equal(row.historico.length, 0);
  assert.equal(row.imagens?.length, 0);
  assert.ok(row.dadosMascarados);
  assert.ok(!JSON.stringify(h.get()).includes("11144477735"));
  assert.equal((await h.engine.risk()).pontuacao, 31);
});
test("Bloqueio de log soma 15 e não guarda valor original", async () => {
  const h = harness();
  await h.engine.create(
    {
      bairroId: 1,
      categoriaId: 1,
      descricao: "senha: ficticia",
      origem: "APP",
    },
    { ...defaults, unsafeLog: true },
  );
  const risk = await h.engine.risk();
  assert.equal(risk.pontuacao, 50);
  assert.equal(risk.vazamentosBloqueados, 1);
  assert.ok(!JSON.stringify(h.get()).includes("ficticia"));
});
test("Status e histórico são gravados juntos; rollback conserva ambos", async () => {
  const { engine } = harness();
  const initial = await engine.detail(1);
  await assert.rejects(
    engine.changeStatus(1, "EM_ANALISE", "VictorHugo_RM554787", {
      ...defaults,
      rollback: true,
    }),
  );
  assert.deepEqual(await engine.detail(1), initial);
  const row = await engine.changeStatus(
    1,
    "EM_ANALISE",
    "VictorHugo_RM554787",
    defaults,
  );
  assert.equal(row.status, "EM_ANALISE");
  assert.deepEqual(row.historico, [
    {
      statusAnterior: "ABERTA",
      statusNovo: "EM_ANALISE",
      alteradoEm: now.toISOString(),
      executor: "VictorHugo_RM554787",
    },
  ]);
});
test("Executor inexistente e ocorrência 99999 retornam erro", async () => {
  const { engine } = harness();
  await assert.rejects(engine.detail(99999), /Ocorrência não encontrada/);
  await assert.rejects(
    engine.changeStatus(1, "EM_ANALISE", "inexistente", defaults),
    /Executor não cadastrado/,
  );
});
test("Anexo persiste metadados e conteúdo sem recriar ocorrência", async () => {
  const h = harness();
  const meta = await h.engine.upload(
    1,
    demoPhoto,
    "Teste",
    "VictorHugo_RM554787",
  );
  assert.equal((await h.engine.list()).length, 3);
  assert.equal((await h.engine.detail(1)).imagens?.[0].id, meta.id);
  assert.equal((await h.engine.photo(meta.id))?.uri, demoPhoto.uri);
});
test("Imagem renomeada, vazia e >2 MiB são rejeitadas; JPEG de teste é válido", () => {
  assert.ok(
    validateImageBytes(new TextEncoder().encode("texto falso"), "image/png"),
  );
  assert.ok(validateImageBytes(new Uint8Array(), "image/jpeg"));
  assert.ok(
    validateImageBytes(new Uint8Array(MAX_PHOTO_BYTES + 1), "image/jpeg"),
  );
  assert.equal(
    validateImageBytes(readFileSync("tests/foto-teste.jpg"), "image/jpeg"),
    null,
  );
});
test("Descrição obrigatória sem mínimo artificial", () => {
  assert.ok(validateDescription(" "));
  assert.equal(validateDescription("Buraco"), null);
});
test("Headers de teste desativados fora do ambiente de teste", () => {
  const flags = { ...defaults, trace: true, rollback: true, unsafeLog: true };
  assert.deepEqual(testHeaders(flags, false, "create"), {});
  assert.deepEqual(testHeaders(flags, false, "status"), {});
  assert.equal(
    testHeaders(flags, true, "status")["X-Simular-Falha"],
    "apos-escritas",
  );
  assert.equal(
    testHeaders(flags, true, "create")["X-Simular-Log-Inseguro"],
    "true",
  );
});
test("API não permite resposta com descrição pessoal crua", () => {
  assert.throws(
    () =>
      parseOccurrence({
        ...makeSeed(now).rows[0],
        descricao: "CPF 11144477735",
      }),
    /sem tratamento/,
  );
  assert.throws(
    () => parseRisk({ ...riskFromEvents([], now), pontuacao: 99 }),
    /não corresponde/,
  );
  assert.equal(
    parseExecutors({
      integrantes: [{ executor: "VictorHugo_RM554787", nome: "Victor" }],
    })[0].id,
    "VictorHugo_RM554787",
  );
});
test("SQLite real: salva e recupera após fechar/reabrir conexão", () => {
  const dir = mkdtempSync(join(tmpdir(), "zelasp-"));
  try {
    const file = join(dir, "test.db");
    let db = new DatabaseSync(file);
    db.exec(schema);
    db.prepare(upsert).run("snapshot", JSON.stringify(makeSeed(now)));
    db.close();
    db = new DatabaseSync(file);
    const row = db
      .prepare("SELECT valor FROM app_storage WHERE chave=?")
      .get("snapshot") as { valor: string };
    assert.equal(JSON.parse(row.valor).rows.length, 3);
    db.close();
  } finally {
    rmSync(dir, { recursive: true });
  }
});
