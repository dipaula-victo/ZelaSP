const { chromium } = await import(
  process.env.PLAYWRIGHT_MODULE || "@playwright/test"
);
import { createServer } from "node:http";
import { readFile, stat, mkdir, writeFile } from "node:fs/promises";
import { existsSync } from "node:fs";
import path from "node:path";
import assert from "node:assert/strict";
const root = path.resolve("dist");
const mime = {
  ".js": "text/javascript",
  ".html": "text/html",
  ".png": "image/png",
  ".ttf": "font/ttf",
  ".json": "application/json",
  ".css": "text/css",
};
const server = createServer(async (req, res) => {
  try {
    let file = path.join(
      root,
      decodeURIComponent(new URL(req.url, "http://localhost").pathname),
    );
    try {
      if (!(await stat(file)).isFile()) file = path.join(root, "index.html");
    } catch {
      file = path.join(root, "index.html");
    }
    res.setHeader(
      "Content-Type",
      mime[path.extname(file)] ?? "application/octet-stream",
    );
    res.end(await readFile(file));
  } catch {
    res.writeHead(500);
    res.end();
  }
});
await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
const port = server.address().port;
const url = `http://127.0.0.1:${port}`;
const browser = await chromium.launch({
  headless: true,
  ...(process.env.CHROMIUM_PATH
    ? { executablePath: process.env.CHROMIUM_PATH }
    : existsSync("/usr/bin/chromium")
      ? { executablePath: "/usr/bin/chromium" }
      : {}),
  args: ["--no-sandbox", "--disable-dev-shm-usage"],
});
const errors = [];
const results = [];
const context = await browser.newContext({
  viewport: { width: 390, height: 844 },
  deviceScaleFactor: 1,
  recordVideo: { dir: "docs/recordings", size: { width: 390, height: 844 } },
});
const page = await context.newPage();
page.on("pageerror", (e) => errors.push(e.message));
page.on("console", (message) => {
  if (message.type() === "error") errors.push(message.text());
});
const shots = path.resolve("docs/screenshots");
await mkdir(shots, { recursive: true });
const check = async (name, fn) => {
  await fn();
  results.push(name);
  console.log("PASS", name);
};
const shot = async (name) => {
  await page.evaluate(() => document.fonts.ready);
  await page.screenshot({ path: path.join(shots, name), fullPage: false });
};
try {
  await page.goto(url);
  await page
    .getByRole("button", { name: "Registrar ocorrência", exact: true })
    .waitFor();
  await page.getByText("#ZSP-1", { exact: false }).waitFor();
  await shot("01-inicio.png");
  await check("Seed e navegação", async () =>
    assert.equal(await page.getByText("ZelaSP", { exact: true }).count(), 1),
  );
  await page
    .getByRole("button", { name: "Registrar ocorrência", exact: true })
    .click();
  await page
    .getByRole("button", { name: "Enviar relato", exact: true })
    .click();
  await page.getByText("A descrição é obrigatória.", { exact: true }).waitFor();
  await page
    .getByRole("button", { name: "Lixeira cheia", exact: true })
    .click();
  await page
    .getByRole("button", { name: "Bairro Teste Centro", exact: true })
    .click();
  await page
    .getByRole("textbox", { name: "Descrição", exact: true })
    .fill("Lixeira cheia. CPF fictício de teste 11144477735");
  await shot("02-cadastro.png");
  await page
    .getByRole("button", { name: "Enviar relato", exact: true })
    .click();
  await page.getByText("Protocolo #ZSP-4", { exact: true }).waitFor();
  await page
    .getByText("Dados pessoais detectados foram mascarados", { exact: true })
    .waitFor();
  await shot("03-detalhe.png");
  await check("Cadastro sem foto, protocolo e mascaramento", async () =>
    assert.equal(
      await page
        .getByText("Lixeira cheia. CPF fictício de teste [CPF OCULTADO]", {
          exact: true,
        })
        .count(),
      1,
    ),
  );
  await page
    .getByRole("button", { name: "Victor Hugo de Paula", exact: true })
    .click();
  await page
    .getByRole("button", { name: "Salvar status", exact: true })
    .click();
  await page
    .getByRole("button", { name: "Confirmar alteração", exact: true })
    .click();
  await page
    .getByText("Status atualizado com sucesso.", { exact: true })
    .waitFor();
  await check(
    "Atualização com histórico",
    async () =>
      await page.getByText("Aberta → Em análise", { exact: true }).waitFor(),
  );
  const chooserPromise = page.waitForEvent("filechooser");
  await page
    .getByRole("button", { name: "Escolher foto", exact: true })
    .click();
  const chooser = await chooserPromise;
  await chooser.setFiles(path.resolve("tests/foto-teste.jpg"));
  await page
    .getByRole("img", { name: "Prévia da foto selecionada", exact: true })
    .waitFor();
  await page.getByRole("button", { name: "Enviar foto", exact: true }).click();
  await page.getByText("Foto anexada com sucesso.", { exact: true }).waitFor();
  await page.reload();
  await page
    .getByRole("img", { name: "Foto da ocorrência", exact: true })
    .waitFor();
  await check("Foto persiste após recarregar", async () =>
    assert.equal(
      await page
        .getByRole("img", { name: "Foto da ocorrência", exact: true })
        .count(),
      1,
    ),
  );
  await page.goto(`${url}/risco`);
  await page.getByText("31", { exact: true }).waitFor();
  await shot("04-risco.png");
  await page.goto(`${url}/ajustes`);
  await page.getByRole("switch", { name: "Sem conexão", exact: true }).check();
  await page.goto(`${url}/ocorrencias`);
  await page
    .getByText("Sem conexão — dados podem estar desatualizados", {
      exact: true,
    })
    .waitFor();
  await page
    .getByRole("button", { name: "Nova ocorrência", exact: true })
    .waitFor();
  assert.ok(
    await page
      .getByRole("button", { name: "Nova ocorrência", exact: true })
      .isDisabled(),
  );
  await shot("05-offline.png");
  await check(
    "Cache e bloqueio de escrita offline",
    async () => await page.getByText("#ZSP-4", { exact: false }).waitFor(),
  );
  await check("Sem erros JavaScript", async () => assert.deepEqual(errors, []));
  await writeFile(
    "docs/browser-test-results.json",
    JSON.stringify(
      { results, errors, platform: "Chromium / React Native Web / IndexedDB" },
      null,
      2,
    ),
  );
} finally {
  await context.close();
  await browser.close();
  await new Promise((resolve) => server.close(resolve));
}
