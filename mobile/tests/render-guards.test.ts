import test from "node:test";
import assert from "node:assert/strict";
import React from "react";
import { readFileSync, existsSync } from "node:fs";
import path from "node:path";
import { createRequire } from "node:module";
const req = createRequire(import.meta.url);
const { renderToStaticMarkup } = req("react-dom/server") as {
  renderToStaticMarkup: (node: React.ReactNode) => string;
};
const babel = req("@babel/core");
// Renderiza as telas reais com primitivas controladas: View rejeita textos
// como o React Native Web em desenvolvimento. Não é um teste de layout visual.
let state: any;
let params: any = {};
function View({ children }: any) {
  for (const child of React.Children.toArray(children)) {
    assert.notEqual(
      typeof child,
      "string",
      "Texto direto em View (inclusive string vazia)",
    );
    assert.notEqual(typeof child, "number", "Número direto em View");
  }
  return React.createElement("div", null, children);
}
const noop = () => {};
function load(file: string, cache = new Map<string, any>()): any {
  if (cache.has(file)) return cache.get(file);
  const code = babel.transformSync(readFileSync(file, "utf8"), {
    filename: file,
    babelrc: false,
    configFile: false,
    plugins: [
      [
        req.resolve("@babel/plugin-transform-typescript"),
        { isTSX: file.endsWith(".tsx") },
      ],
      [
        req.resolve("@babel/plugin-transform-react-jsx"),
        { runtime: "automatic" },
      ],
      req.resolve("@babel/plugin-transform-modules-commonjs"),
    ],
  }).code;
  const mod = { exports: {} };
  cache.set(file, mod.exports);
  function local(id: string): any {
    if (id === "react-native")
      return {
        View,
        ScrollView: View,
        KeyboardAvoidingView: View,
        Pressable: View,
        Text: ({ children }: any) =>
          React.createElement("span", null, children),
        TextInput: () => null,
        Image: () => null,
        Switch: () => null,
        ActivityIndicator: () => null,
        Modal: ({ visible, children }: any) =>
          visible ? React.createElement(View, null, children) : null,
        StyleSheet: { create: (x: any) => x, hairlineWidth: 1 },
        Platform: { OS: "web" },
        useWindowDimensions: () => ({ width: 390, height: 844 }),
      };
    if (id === "react-native-safe-area-context") return { SafeAreaView: View };
    if (id === "@expo/vector-icons/Ionicons") return () => null;
    if (id === "expo-router")
      return {
        router: { push: noop, replace: noop, back: noop },
        useLocalSearchParams: () => params,
        useFocusEffect: noop,
      };
    if (id.endsWith("/state/AppContext"))
      return { useApp: () => state, useRows: () => state.rows ?? [] };
    if (id.endsWith("/services/gateway") || id.endsWith("/services/photos"))
      return {};
    if (id.startsWith(".")) {
      let p = path.resolve(path.dirname(file), id);
      if (!existsSync(p) || !path.extname(p)) {
        p = [p + ".tsx", p + ".ts", path.join(p, "index.ts")].find(existsSync)!;
      }
      return load(p, cache);
    }
    return req(id);
  }
  new Function("require", "module", "exports", code)(local, mod, mod.exports);
  cache.set(file, mod.exports);
  return mod.exports;
}
function baseState() {
  return {
    ready: true,
    fatal: "",
    error: "",
    loading: false,
    offline: false,
    busy: false,
    stale: false,
    snapshot: {
      rows: [],
      bairros: [],
      categorias: [],
      executores: [],
      photos: {},
      myIds: [],
      updatedAt: null,
      risk: null,
      riskUpdatedAt: null,
    },
    settings: { offline: false, fail: false, empty: false },
    refresh: async () => {},
    refreshRisk: async () => {},
    setSettings: async () => {},
    create: async () => {},
    detail: async () => {},
  };
}
for (const route of [
  "(tabs)/index",
  "(tabs)/ocorrencias",
  "(tabs)/ajustes",
  "(tabs)/risco",
  "nova",
  "ocorrencia/[id]",
]) {
  test(`${route}: renderiza sem strings soltas quando mensagens estão vazias`, () => {
    state = baseState();
    params = {};
    const Screen = load(path.resolve("app", route + ".tsx")).default;
    assert.doesNotThrow(() =>
      renderToStaticMarkup(React.createElement(Screen)),
    );
  });
}
test("Page aceita títulos vazios sem criar texto fora de Text", () => {
  const { Page } = load(path.resolve("src/components/ui.tsx"));
  assert.doesNotThrow(() =>
    renderToStaticMarkup(
      React.createElement(Page, { title: "", subtitle: "" }),
    ),
  );
});

test("o verificador reproduz o problema de string vazia do JSX antigo", () => {
  const message = "";
  assert.throws(
    () =>
      renderToStaticMarkup(
        React.createElement(
          View,
          null,
          message && React.createElement("span", null, "erro"),
        ),
      ),
    /Texto direto em View/,
  );
});
