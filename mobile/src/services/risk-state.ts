import type { Risk, Snapshot } from "../data/types";
import { parseCachedRisk, parseRisk } from "./contract";
import { RISK_UNAVAILABLE, ServiceError } from "./errors";

// Reused by the provider and isolated tests. Scope includes mode and settings.
export function createRiskCoordinator() {
  let generation = 0;
  let pending = Promise.resolve();
  return {
    invalidate() { generation++; },
    async refresh(scope: () => string, fetch: () => Promise<unknown>,
      commit: (risk: Risk) => Promise<void>): Promise<Risk | null> {
      const ticket = ++generation;
      const origin = scope();
      const current = () => ticket === generation && scope() === origin;
      try {
        const value = await fetch();
        if (!current()) return null;
        const risk = parseRisk(value, origin.startsWith("api:"));
        let applied = false;
        const task = pending.catch(() => {}).then(async () => {
          if (!current()) return;
          await commit(risk);
          applied = true;
        });
        pending = task;
        await task;
        return applied && current() ? risk : null;
      } catch {
        if (!current()) return null;
        throw new ServiceError(RISK_UNAVAILABLE);
      }
    },
  };
}

// Must complete the rewrite before publishing cached state, even when offline.
export async function restoreRiskCache(read: () => Promise<Snapshot>,
  write: (snapshot: Snapshot) => Promise<void>): Promise<Snapshot> {
  try {
    const cache = await read();
    if (!cache || typeof cache !== "object" || Array.isArray(cache)) throw new Error();
    const risk = parseCachedRisk(cache.risk);
    const safe = { ...cache, risk,
      riskUpdatedAt: risk && typeof cache.riskUpdatedAt === "string" &&
        Number.isFinite(Date.parse(cache.riskUpdatedAt))
        ? new Date(cache.riskUpdatedAt).toISOString() : null };
    await write(safe);
    return safe;
  } catch {
    // Fail closed: do not publish untrusted cache or expose storage messages.
    throw new ServiceError(RISK_UNAVAILABLE);
  }
}
