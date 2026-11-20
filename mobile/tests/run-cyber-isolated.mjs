// Dependency-free fallback for Node >=24. Uses native type stripping, not type checking.
// No Expo/browser/server is started. HTTP tests replace fetch with in-memory responses.
import { registerHooks } from "node:module";
import { fileURLToPath } from "node:url";
process.chdir(fileURLToPath(new URL("../", import.meta.url)));
globalThis.fetch = async () => { throw new Error("Real network disabled in isolated Cybersecurity tests."); };
registerHooks({
  resolve(specifier, context, next) {
    try { return next(specifier, context); }
    catch (error) {
      if (error.code === "ERR_MODULE_NOT_FOUND" && specifier.startsWith(".") && !specifier.endsWith(".ts"))
        return next(specifier + ".ts", context);
      throw error;
    }
  },
});
await import("./core.test.ts");
await import("./risk-api.test.ts");
await import("./risk-state.test.ts");
