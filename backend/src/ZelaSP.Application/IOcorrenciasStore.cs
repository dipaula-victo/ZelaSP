using ZelaSP.Domain;

namespace ZelaSP.Application;

public interface IOcorrenciasStore
{
    Task<IReadOnlyList<BairroResponse>> ListarBairrosAsync(CancellationToken ct);
    Task<IReadOnlyList<CategoriaOcorrenciaResponse>> ListarCategoriasAsync(CancellationToken ct);
    Task<bool> BairroExisteAsync(int id, CancellationToken ct);
    Task<bool> CategoriaExisteAsync(int id, CancellationToken ct);
    Task<OcorrenciaResponse?> ObterAsync(int id, CancellationToken ct);
    Task<PaginaOcorrenciasResponse> ListarAsync(ListarOcorrenciasQuery query, CancellationToken ct);
    Task<OcorrenciaResponse> CriarAsync(Ocorrencia ocorrencia, CancellationToken ct, IReadOnlyList<EventoSeguranca>? eventos = null);
    Task AlterarStatusComHistoricoAsync(int id, StatusOcorrencia anterior, StatusOcorrencia novo,
        DateTime alteradoEm, string executor, CancellationToken ct);
    Task<IReadOnlyList<HistoricoStatusResponse>> HistoricoAsync(int id, CancellationToken ct);
}
