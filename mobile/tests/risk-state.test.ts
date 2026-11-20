import test from "node:test";
import assert from "node:assert/strict";
import { createRiskCoordinator, restoreRiskCache } from "../src/services/risk-state";
import { emptySnapshot } from "../src/data/types";
import { maskDescription, riskFromEvents } from "../src/services/privacy";
import { mockRiskExplanation } from "../src/services/risk-client";
import { RISK_UNAVAILABLE } from "../src/services/errors";
import { createMockEngine, type MockState } from "../src/services/mock-engine";
import { defaults } from "../src/data/types";

const risk = () => mockRiskExplanation(riskFromEvents([]));
function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>(done => { resolve = done; });
  return { promise, resolve };
}
test("out of order responses never replace latest valid risk", async () => {
  const c = createRiskCoordinator(), a = deferred<unknown>(), b = deferred<unknown>();
  const saved: unknown[] = [];
  const commit = async (value: unknown) => { saved.push(value); };
  const first = c.refresh(() => "mock:", () => a.promise, commit);
  const second = c.refresh(() => "mock:", () => b.promise, commit);
  b.resolve(risk()); await second;
  a.resolve(risk()); assert.equal(await first, null);
  assert.equal(saved.length, 1);
});
for (const [from, to] of [["api:", "mock:"], ["mock:", "api:"]])
  test(`scope switch ${from} to ${to} discards pending response`, async () => {
    let scope = from, calls = 0;
    const c = createRiskCoordinator(), d = deferred<unknown>();
    const work = c.refresh(() => scope, () => d.promise, async () => { calls++; });
    scope = to; d.resolve(risk());
    assert.equal(await work, null); assert.equal(calls, 0);
  });
test("invalidation also protects switch away and back", async () => {
  const c = createRiskCoordinator(), d = deferred<unknown>();
  const work = c.refresh(() => "mock:", () => d.promise, async () => assert.fail());
  c.invalidate(); d.resolve(risk()); assert.equal(await work, null);
});
test("obsolete request queued behind disk write is discarded", async () => {
  const c = createRiskCoordinator(), disk = deferred<void>();
  const committed: number[] = [];
  const a = c.refresh(() => "mock:", async () => risk(), async () => {
    committed.push(1); await disk.promise;
  });
  await new Promise(resolve => setImmediate(resolve));
  const b = c.refresh(() => "mock:", async () => risk(), async () => { committed.push(2); });
  await new Promise(resolve => setImmediate(resolve));
  const last = c.refresh(() => "mock:", async () => risk(), async () => { committed.push(3); });
  disk.resolve(); await Promise.all([a, b, last]);
  assert.deepEqual(committed, [1, 3]);
});
test("fallback validated and persisted; unavailability preserves it", async () => {
  const c = createRiskCoordinator(); let saved = risk();
  await c.refresh(() => "mock:", async () => risk(), async value => { saved = value; });
  const prior = saved;
  await assert.rejects(c.refresh(() => "mock:", async () => { throw new Error("private"); },
    async value => { saved = value; }), { message: RISK_UNAVAILABLE });
  assert.equal(saved, prior);
});
test("invalid response cannot reach persistence", async () => {
  const c = createRiskCoordinator();
  await assert.rejects(c.refresh(() => "mock:", async () => ({ ...risk(), pontuacao: 99 }),
    async () => assert.fail()), { message: RISK_UNAVAILABLE });
});
for (const value of [{ bad: true }, { ...risk(), explicacao: { origem: "LLM", texto: "senha: Segredo Ficticio" } }])
  test("invalid cache is rewritten before publication, other data preserved", async () => {
    const cache = { ...emptySnapshot, myIds: [7], risk: value } as any;
    let stored: any;
    const safe = await restoreRiskCache(async () => cache, async v => { stored = structuredClone(v); });
    assert.equal(safe.risk, null); assert.equal(stored.risk, null);
    assert.deepEqual(stored.myIds, [7]);
    assert.ok(!JSON.stringify(stored).includes("Segredo"));
  });
test("unknown fields are removed before storage and publication", async () => {
  const cached = { ...emptySnapshot, risk: { ...risk(), secret: "PrivateFixture" } };
  let stored = "";
  const safe = await restoreRiskCache(async () => cached, async value => { stored = JSON.stringify(value); });
  assert.ok(!stored.includes("PrivateFixture")); assert.ok(!("secret" in safe.risk!));
});
for (const stage of ["read", "write"])
  test(`storage ${stage} failure is generic and fails closed`, async () => {
    await assert.rejects(restoreRiskCache(async () => {
      if (stage === "read") throw new Error("private read details");
      return { ...emptySnapshot, risk: risk() };
    }, async () => { throw new Error("private write details"); }), { message: RISK_UNAVAILABLE });
  });
test("failed commit does not poison following risk updates", async () => {
  const c = createRiskCoordinator();
  await assert.rejects(c.refresh(() => "mock:", async () => risk(), async () => { throw new Error(); }));
  assert.ok(await c.refresh(() => "mock:", async () => risk(), async () => {}));
});
test("mock persistence and returned description contain no password fragments", async () => {
  let stored: MockState | null = null;
  const engine = createMockEngine({
    read: async () => stored,
    write: async value => { stored = structuredClone(value); },
  }, () => new Date("2026-11-19T12:00:00Z"));
  const row = await engine.create({ bairroId: 1, categoriaId: 1,
    descricao: "senha: PrimeiraFicticia SegundaFicticia; calcada obstruida", origem: "APP" }, defaults);
  for (const data of [JSON.stringify(stored), JSON.stringify(row)]) {
    assert.ok(!data.includes("PrimeiraFicticia"));
    assert.ok(!data.includes("SegundaFicticia"));
    assert.ok(data.includes("calcada obstruida"));
  }
});
for (const [input, expected] of [
  ["senha: Primeira Segunda!?.@", "senha: [SENHA OCULTADA]"],
  ["senha: Primeira Segunda, rua ficticia", "senha: [SENHA OCULTADA], rua ficticia"],
  ["senha=Primeira Segunda; fim", "senha=[SENHA OCULTADA]; fim"],
  ["senha: Primeira Segunda\ntexto", "senha: [SENHA OCULTADA]\ntexto"],
  ['senha: "Primeira Segunda" fim', 'senha: "[SENHA OCULTADA]" fim'],
  ["senha: [CPF OCULTADO] Segunda", "senha: [SENHA OCULTADA]"],
  ["senha: [CPF OCULTADO]", "senha: [CPF OCULTADO]"],
  ["senha: [SENHA OCULTADA] Segunda", "senha: [SENHA OCULTADA]"],
  ["senha: Primeira senha=Segunda Terceira", "senha: [SENHA OCULTADA] senha=[SENHA OCULTADA]"],
  ["senha: teste@example.test 11144477735", "senha: [SENHA OCULTADA]"],
] as const)
  test(`password policy fixture ${expected}`, () => {
    const masked = maskDescription(input);
    assert.equal(masked.text, expected);
    assert.equal(maskDescription(masked.text).text, expected);
    assert.deepEqual(maskDescription(masked.text).detections, []);
  });
