using ZelaSP.Domain;

namespace ZelaSP.Application;

public sealed class IoTService(IIoTStore store, TimeProvider clock)
{
    public Task<IReadOnlyList<DispositivoResponse>> ListarDispositivosAsync(CancellationToken ct) => store.ListarDispositivosAsync(ct);

    public async Task<LeituraSensorResponse> RegistrarAsync(RegistrarLeituraRequest request, CancellationToken ct)
    {
        var erros = new Dictionary<string, string[]>();
        if (request.DispositivoId is not > 0) erros["dispositivoId"] = ["Informe um identificador inteiro positivo."];
        if (request.NivelPercentual is not (>= 0m and <= 100m)) erros["nivelPercentual"] = ["Informe um número entre 0 e 100."];
        else if (decimal.Round(request.NivelPercentual.Value, 2) != request.NivelPercentual.Value)
            erros["nivelPercentual"] = ["Informe no máximo duas casas decimais efetivas, sem arredondamento."];
        if (erros.Count > 0) throw new ValidacaoAplicacaoException(erros);
        if (!await store.DispositivoExisteAsync(request.DispositivoId!.Value, ct))
            throw new RecursoNaoEncontradoException("Dispositivo não encontrado.");
        var recebido = clock.GetUtcNow().UtcDateTime;
        var utcMilissegundos = new DateTime(recebido.Ticks - recebido.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        return await store.RegistrarAsync(new LeituraSensor
        {
            DispositivoId = request.DispositivoId.Value, NivelPercentual = request.NivelPercentual!.Value, LidaEm = utcMilissegundos
        }, ct);
    }
}
