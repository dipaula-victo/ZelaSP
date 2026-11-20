import type { PiiType, SecurityEvent, Risk } from "../data/types";
export const labels: Record<PiiType, string> = {
  CPF: "[CPF OCULTADO]",
  TELEFONE: "[TELEFONE OCULTADO]",
  EMAIL: "[E-MAIL OCULTADO]",
  SENHA: "[SENHA OCULTADA]",
  SAUDE: "[SAÚDE OCULTADA]",
};
export const weights: Record<PiiType, number> = {
  CPF: 6,
  TELEFONE: 3,
  EMAIL: 3,
  SENHA: 10,
  SAUDE: 8,
};
export const alerts: Record<PiiType | "VAZAMENTO", string> = {
  CPF: "Um CPF foi digitado em um relato e foi ocultado. O CPF pode ser usado em fraudes: não o informe em relatos.",
  TELEFONE:
    "Um telefone foi digitado em um relato e foi ocultado. Evite informar telefone em relatos.",
  EMAIL:
    "Um e-mail foi digitado em um relato e foi ocultado. Evite informar e-mail em relatos.",
  SENHA:
    "Uma senha foi digitada em um relato e foi ocultada. Se for sua, troque-a agora.",
  SAUDE:
    "Uma informação de saúde foi digitada em um relato e foi ocultada. Dados de saúde são sensíveis: não os informe.",
  VAZAMENTO:
    "O sistema impediu que um dado pessoal aparecesse em um local inseguro.",
};
export function validCPF(value: string) {
  const digits = value.replace(/\D/g, "");
  if (digits.length !== 11 || /^(\d)\1+$/.test(digits)) return false;
  for (let n = 9; n <= 10; n++) {
    let sum = 0;
    for (let i = 0; i < n; i++) sum += Number(digits[i]) * (n + 1 - i);
    const check = ((sum * 10) % 11) % 10;
    if (check !== Number(digits[n])) return false;
  }
  return true;
}
// O texto original só existe em memória. O retorno contém apenas texto tratado e tipos.
export function maskDescription(input: string) {
  const detections: PiiType[] = [];
  let text = input;
  const replace = (type: PiiType) => {
    detections.push(type);
    return labels[type];
  };
  // Password spans take priority. Quotes remain to preserve boundaries on replay.
  const marker = /\bsenha\s*(?::|=|é)\s*/gi;
  const markers = [...input.matchAll(marker)];
  let end = 0;
  text = "";
  for (let i = 0; i < markers.length; i++) {
    const match = markers[i];
    if (match.index! < end) continue;
    const start = match.index! + match[0].length;
    const line = input.slice(start).search(/[\r\n\u0085\u2028\u2029]/);
    const lineEnd = line < 0 ? input.length : start + line;
    const quote = input[start];
    const closing = quote === '"' || quote === "'" ? input.indexOf(quote, start + 1) : -1;
    const quoted = closing > start && closing < lineEnd;
    let stop = quoted ? closing + 1 : Math.min(markers[i + 1]?.index ?? input.length, lineEnd);
    if (!quoted) {
      const delimiter = input.slice(start, stop).search(/[,;]/);
      if (delimiter >= 0) stop = start + delimiter;
      while (stop > start && /\s/.test(input[stop - 1])) stop--;
    }
    const from = quoted ? start + 1 : start;
    const to = quoted ? stop - 1 : stop;
    const value = input.slice(from, to);
    text += input.slice(end, from);
    text += value && !Object.values(labels).includes(value) ? replace("SENHA") : value;
    end = to;
  }
  text += input.slice(end);
  text = text.replace(/(?<!\d)\d{3}\.?\d{3}\.?\d{3}-?\d{2}(?!\d)/g, (value) =>
    validCPF(value) ? replace("CPF") : value,
  );
  text = text.replace(/\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b/gi, () =>
    replace("EMAIL"),
  );
  text = text.replace(
    /(?<![\d\w])(?:\+?55[\s.-]*)?\(?[1-9]\d\)?[\s.-]*(?:9\d{4}|[2-5]\d{3})[\s-]*\d{4}(?!\d)/g,
    () => replace("TELEFONE"),
  );
  text = text.replace(
    /(?<![\p{L}\p{N}])(?:diabetes|hipertens[aã]o|c[aâ]ncer|HIV|depress[aã]o|epilepsia)(?![\p{L}\p{N}])/giu,
    () => replace("SAUDE"),
  );
  return { text, detections };
}
export function hasMask(text: string) {
  return Object.values(labels).some((label) => text.includes(label));
}
export function riskFromEvents(
  events: SecurityEvent[],
  now = new Date(),
): Risk {
  const start = now.getTime() - 7 * 86400000;
  const valid = events.filter((e) => {
    const t = Date.parse(e.ocorridoEm);
    return t >= start && t <= now.getTime();
  });
  const deteccoes = { cpf: 0, telefone: 0, email: 0, senha: 0, saude: 0 };
  let points = 0,
    blocked = 0;
  for (const e of valid) {
    if (e.tipoEvento === "VAZAMENTO_BLOQUEADO") {
      blocked++;
      points += 15;
    } else {
      points += weights[e.tipoDado];
      deteccoes[e.tipoDado.toLowerCase() as keyof typeof deteccoes]++;
    }
  }
  const pontuacao = Math.min(100, points);
  return {
    pontuacao,
    nivel: pontuacao < 20 ? "BAIXO" : pontuacao < 50 ? "MEDIO" : "ALTO",
    periodoDias: 7,
    deteccoes,
    vazamentosBloqueados: blocked,
    alertas: [
      ...(Object.keys(weights) as PiiType[])
        .filter((k) => deteccoes[k.toLowerCase() as keyof typeof deteccoes] > 0)
        .map((tipo) => ({ tipo, texto: alerts[tipo] })),
      ...(blocked ? [{ tipo: "VAZAMENTO", texto: alerts.VAZAMENTO }] : []),
    ],
  };
}
