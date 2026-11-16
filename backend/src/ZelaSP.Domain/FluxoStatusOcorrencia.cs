namespace ZelaSP.Domain;

public static class FluxoStatusOcorrencia
{
    public const StatusOcorrencia Inicial = StatusOcorrencia.ABERTA;

    public static bool Permite(StatusOcorrencia atual, StatusOcorrencia novo) =>
        (atual, novo) is (StatusOcorrencia.ABERTA, StatusOcorrencia.EM_ANALISE)
            or (StatusOcorrencia.EM_ANALISE, StatusOcorrencia.RESOLVIDA);
}
