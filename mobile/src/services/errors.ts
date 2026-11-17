export class ServiceError extends Error {
  status: number;
  uncertain: boolean;
  constructor(message: string, status = 0, uncertain = false) {
    super(message);
    this.status = status;
    this.uncertain = uncertain;
  }
}
export const OFFLINE = "Sem conexão — dados podem estar desatualizados";
export const UNAVAILABLE =
  "Não foi possível acessar o serviço agora. Tente novamente em instantes.";
export const RISK_UNAVAILABLE =
  "Não foi possível carregar o Risco Cidadão agora. Tente novamente em instantes.";
