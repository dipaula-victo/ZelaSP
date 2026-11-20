import test, { after, before } from "node:test";
import assert from "node:assert/strict";
import { parseRisk, parseCachedRisk } from "../src/services/contract";
import { alerts } from "../src/services/privacy";
import { RISK_UNAVAILABLE } from "../src/services/errors";

// Only this process is configured. Every HTTP test replaces fetch; no real network.
const previousUrl = process.env.EXPO_PUBLIC_API_URL;
process.env.EXPO_PUBLIC_API_URL = "http://127.0.0.1:5187";
let fetchRisk: typeof import("../src/services/risk-client").fetchRisk;
let mockRiskExplanation: typeof import("../src/services/risk-client").mockRiskExplanation;
before(async () => {
  ({ fetchRisk, mockRiskExplanation } = await import("../src/services/risk-client"));
});
after(() => {
  if (previousUrl === undefined) delete process.env.EXPO_PUBLIC_API_URL;
  else process.env.EXPO_PUBLIC_API_URL = previousUrl;
});

function response(origem: "LLM" | "FALLBACK" = "LLM") {
  return {
    pontuacao: 30, nivel: "MEDIO", periodoDias: 7,
    deteccoes: { cpf: 1, telefone: 1, email: 1, senha: 1, saude: 1 },
    vazamentosBloqueados: 0,
    alertas: (["CPF", "TELEFONE", "EMAIL", "SENHA", "SAUDE"] as const)
      .map(tipo => ({ tipo, texto: alerts[tipo] })),
    janela: { inicioUtc: "2026-11-12T12:00:00Z", fimUtc: "2026-11-19T12:00:00Z" },
    explicacao: { texto: "Dados pessoais foram ocultados nos relatos. Evite informar dados pessoais. O indicador não representa perigo no bairro.", origem },
  };
}

for (const origem of ["LLM", "FALLBACK"] as const) {
  test(`contrato real preserva explicação ${origem}, pontos e janela UTC`, () => {
    const input = response(origem);
    assert.deepEqual(parseRisk(input, true), input);
    assert.equal(parseRisk(input, true).pontuacao, 30);
  });
}

test("bloqueio reconhecido soma apenas 15 e preserva o alerta agregado", () => {
  const base = response();
  const input = { ...base, pontuacao: 45, vazamentosBloqueados: 1,
    alertas: [...base.alertas, { tipo: "VAZAMENTO", texto: alerts.VAZAMENTO }] };
  assert.deepEqual(parseRisk(input, true), input);
});

test("pontuação máxima aceita contagens grandes sem cálculo inseguro", () => {
  const base = response();
  const input = { ...base, pontuacao: 100, nivel: "ALTO",
    deteccoes: { ...base.deteccoes, cpf: Number.MAX_SAFE_INTEGER } };
  assert.deepEqual(parseRisk(input, true), input);
});

test("banco sem eventos mantém risco baixo e fallback explícito", () => {
  const input = { ...response("FALLBACK"), pontuacao: 0, nivel: "BAIXO",
    deteccoes: { cpf: 0, telefone: 0, email: 0, senha: 0, saude: 0 }, alertas: [] };
  assert.deepEqual(parseRisk(input, true), input);
});

test("resposta é reconstruída sem campos livres, categorias ou metadados extras", () => {
  const input = response();
  const parsed = parseRisk({ ...input, descricao: "SENTINELA_NAO_PERSISTIR",
    deteccoes: { ...input.deteccoes, "ficticio@example.test": 1 },
    explicacao: { ...input.explicacao, executor: "SENTINELA_NAO_PERSISTIR" },
    alertas: input.alertas.map(a => ({ ...a, detalhe: "SENTINELA_NAO_PERSISTIR" })) }, true);
  assert.deepEqual(parsed, input);
  assert.doesNotMatch(JSON.stringify(parsed), /SENTINELA|ficticio@example/);
});

for (const [indice, texto] of [
  "CPF 11144477735", "Telefone (11) 98888-7777", "ficticio@example.test",
  "senha: SegredoFicticio123", "Informação sobre diabetes", "Informação sobre HIV",
  "Informação sobre ca\u0302ncer", "senha e\u0301 SegredoFicticio123",
  "senha: [CPF OCULTADO] SegredoFicticio123", "[SENHA OCULTADA] SegredoFicticio123",
  "Veja https://example.test", "<script>teste</script>", "Linha\nextra", "Texto\u202Eoculto",
  " ", " Texto", "Texto ", "x".repeat(601),
].entries()) {
  test(`explicação não confiável rejeitada sem ecoar o conteúdo: caso ${indice + 1}`, () => {
    const input = response();
    assert.throws(() => parseRisk({ ...input, explicacao: { texto, origem: "LLM" } }, true),
      error => error instanceof Error && error.message === "A explicação recebida não passou pela validação de segurança.");
  });
}

for (const campo of ["janela", "explicacao"] as const) {
  test(`API exige ${campo}, preservando compatibilidade com registros legados`, () => {
    const input: Record<string, unknown> = response();
    delete input[campo];
    assert.throws(() => parseRisk(input, true));
    assert.doesNotThrow(() => parseRisk(input));
  });
}

for (const janela of [
  null, { inicioUtc: "2026-11-19T12:00:00Z", fimUtc: "2026-11-12T12:00:00Z" },
  { inicioUtc: "2026-11-12T12:00:00-03:00", fimUtc: "2026-11-19T12:00:00Z" },
  { inicioUtc: "2026-11-13T12:00:00Z", fimUtc: "2026-11-19T12:00:00Z" },
  { inicioUtc: "2026-02-31T12:00:00Z", fimUtc: "2026-03-10T12:00:00Z" },
]) {
  test(`janela inválida rejeitada: ${JSON.stringify(janela)}`, () => {
    assert.throws(() => parseRisk({ ...response(), janela }, true));
  });
}

test("janela aceita precisão UTC de datetime2(3) e DateTime com sete casas", () => {
  for (const fraction of [".123", ".1234567"]) {
    const janela = { inicioUtc: `2026-11-12T12:00:00${fraction}Z`, fimUtc: `2026-11-19T12:00:00${fraction}Z` };
    assert.deepEqual(parseRisk({ ...response(), janela }, true).janela, janela);
  }
});

test("origem desconhecida não vira alegação de LLM", () => {
  assert.throws(() => parseRisk({ ...response(), explicacao: { texto: "Texto seguro.", origem: "OUTRO" } }, true));
});

test("cache rejeita explicação pessoal e nunca devolve o valor cru", () => {
  const input = response();
  assert.equal(parseCachedRisk({ ...input, explicacao: { texto: "senha: SegredoFicticio123", origem: "LLM" } }), null);
  assert.equal(parseCachedRisk(null), null);
  assert.equal(parseCachedRisk("SENTINELA"), null);
  assert.deepEqual(JSON.parse(JSON.stringify(parseCachedRisk(input))), input);
});

test("cache legado mantém indicador sem inventar explicação ou origem", () => {
  const { janela, explicacao, ...legacy } = response();
  assert.deepEqual(parseCachedRisk(legacy), legacy);
  assert.equal(parseCachedRisk(legacy)?.explicacao, undefined);
});

test("simulador usa fallback identificado e não fabrica chamada LLM", () => {
  const { janela, explicacao, ...legacy } = response();
  const mock = mockRiskExplanation(legacy);
  assert.equal(mock.explicacao?.origem, "FALLBACK");
  assert.match(mock.explicacao!.texto, /fictícios/);
  assert.equal(mock.pontuacao, legacy.pontuacao);
});

for (const override of [
  { pontuacao: 31 }, { nivel: "ALTO" }, { periodoDias: 8 },
  { vazamentosBloqueados: -1 }, { vazamentosBloqueados: Number.MAX_SAFE_INTEGER + 1 },
  { deteccoes: { ...response().deteccoes, cpf: "1" } },
  { alertas: [null] }, { alertas: [{ tipo: "__proto__", texto: "SENTINELA" }] },
  { alertas: [] }, { alertas: [...response().alertas, response().alertas[0]] },
]) {
  test(`agregados/alertas inconsistentes rejeitados: ${JSON.stringify(override).slice(0, 90)}`, () => {
    assert.throws(() => parseRisk({ ...response(), ...override }, true));
  });
}

test("adapter Mobile usa GET e preserva contrato com fetch simulado", async t => {
  let calls = 0;
  t.mock.method(globalThis, "fetch", async (url: unknown, init?: RequestInit) => {
    calls++;
    assert.equal(url, "http://127.0.0.1:5187/api/risco-cidadao");
    assert.equal(init?.method, "GET");
    assert.equal(init?.body, undefined);
    assert.ok(init?.signal);
    return new Response(JSON.stringify(response()), { status: 200 });
  });
  assert.deepEqual(await fetchRisk(), response());
  assert.equal(calls, 1);
});

for (const caso of ["http500", "network", "abort", "json", "pii", "missing"]) {
  test(`falha ${caso}: mensagem pública genérica e nenhuma repetição`, async t => {
    let calls = 0;
    t.mock.method(globalThis, "fetch", async () => {
      calls++;
      if (caso === "network") throw new Error("senha: SegredoFicticio123");
      if (caso === "abort") throw new DOMException("SENTINELA", "AbortError");
      if (caso === "http500") return new Response(JSON.stringify({ erro: "senha: SegredoFicticio123" }), { status: 500 });
      if (caso === "json") return new Response("{", { status: 200 });
      if (caso === "missing") return new Response(JSON.stringify({ pontuacao: 0 }), { status: 200 });
      return new Response(JSON.stringify({ ...response(), explicacao: { texto: "ficticio@example.test", origem: "LLM" } }), { status: 200 });
    });
    await assert.rejects(fetchRisk(), error => error instanceof Error && error.message === RISK_UNAVAILABLE);
    assert.equal(calls, 1);
  });
}
