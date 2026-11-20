import type { Risk } from "../data/types";
import { parseRisk } from "./contract";
import { request } from "./http";
import { RISK_UNAVAILABLE, ServiceError } from "./errors";

// Separate from native storage/photo modules so this boundary is testable without Expo.
export async function fetchRisk(): Promise<Risk> {
  try {
    return parseRisk(await request("/api/risco-cidadao", { method: "GET" }), true);
  } catch {
    // Do not propagate arbitrary provider/API/network messages to the citizen.
    throw new ServiceError(RISK_UNAVAILABLE);
  }
}

export function mockRiskExplanation(value: unknown): Risk {
  return {
    ...parseRisk(value),
    explicacao: {
      texto: "Este resultado usa eventos fictícios de demonstração. Evite informar dados pessoais nos relatos. O indicador não representa perigo no bairro.",
      origem: "FALLBACK",
    },
  };
}
