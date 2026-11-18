import { apiUrl } from "./config";
import { ServiceError, UNAVAILABLE } from "./errors";
import { maskDescription } from "./privacy";
export async function request<T>(
  path: string,
  options: RequestInit = {},
  mutation = false,
): Promise<T> {
  if (!/^https?:\/\//.test(apiUrl))
    throw new ServiceError("Configure o endereço da API no arquivo .env.");
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), 15000);
  try {
    const response = await fetch(`${apiUrl}${path}`, {
      ...options,
      signal: controller.signal,
    });
    if (!response.ok) {
      let message = UNAVAILABLE;
      try {
        const body = await response.json();
        if (typeof body.erro === "string")
          message = maskDescription(body.erro).text;
      } catch {}
      throw new ServiceError(
        message,
        response.status,
        mutation && response.status >= 500,
      );
    }
    if (response.status === 204) return undefined as T;
    const text = await response.text();
    return text ? (JSON.parse(text) as T) : (undefined as T);
  } catch (e) {
    if (e instanceof ServiceError) throw e;
    throw new ServiceError(
      mutation
        ? "Não foi possível confirmar a operação. Consulte a ocorrência antes de repetir."
        : UNAVAILABLE,
      0,
      mutation,
    );
  } finally {
    clearTimeout(timer);
  }
}
