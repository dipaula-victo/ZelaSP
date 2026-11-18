using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ZelaSP.Domain;

namespace ZelaSP.Application;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegistrarLeituraRequest
{
    [Required, Range(1, int.MaxValue)]
    public int? DispositivoId { get; init; }

    [Required, Range(typeof(decimal), "0", "100")]
    public decimal? NivelPercentual { get; init; }
}

public sealed record UltimaLeituraResponse(int Id, decimal NivelPercentual, DateTime LidaEm, bool NivelCritico);
public sealed record DispositivoResponse(int Id, int BairroId, string Nome, string Local, UltimaLeituraResponse? UltimaLeitura);
public sealed record LeituraSensorResponse(int Id, int DispositivoId, decimal NivelPercentual, DateTime LidaEm, bool NivelCritico);

public interface IIoTStore
{
    Task<IReadOnlyList<DispositivoResponse>> ListarDispositivosAsync(CancellationToken ct);
    Task<bool> DispositivoExisteAsync(int id, CancellationToken ct);
    Task<LeituraSensorResponse> RegistrarAsync(LeituraSensor leitura, CancellationToken ct);
}
