using Microsoft.EntityFrameworkCore;
using ZelaSP.Application;
using ZelaSP.Domain;

namespace ZelaSP.Infrastructure;

public sealed class IoTStore(ZelaSpDbContext db) : IIoTStore
{
    public async Task<IReadOnlyList<DispositivoResponse>> ListarDispositivosAsync(CancellationToken ct)
    {
        // Scalar latest-ID subquery + left join translates on both SQL Server and SQLite,
        // without APPLY, entity materialization or one query per device.
        var linhas = await (from dispositivo in db.Dispositivos.AsNoTracking()
                            join leitura in db.LeiturasSensor.AsNoTracking()
                                on db.LeiturasSensor.Where(l => l.DispositivoId == dispositivo.Id)
                                    .OrderByDescending(l => l.LidaEm).ThenByDescending(l => l.Id)
                                    .Select(l => (int?)l.Id).FirstOrDefault()
                                equals (int?)leitura.Id into ultimas
                            from ultima in ultimas.DefaultIfEmpty()
                            orderby dispositivo.Nome, dispositivo.Id
                            select new
                            {
                                dispositivo.Id, dispositivo.BairroId, dispositivo.Nome, dispositivo.Local,
                                UltimaId = (int?)ultima.Id, Nivel = (decimal?)ultima.NivelPercentual, Data = (DateTime?)ultima.LidaEm
                            }).ToListAsync(ct);
        return linhas.Select(l => new DispositivoResponse(l.Id, l.BairroId, l.Nome, l.Local,
            l.UltimaId is null ? null : new UltimaLeituraResponse(l.UltimaId.Value, l.Nivel!.Value,
                DateTime.SpecifyKind(l.Data!.Value, DateTimeKind.Utc), RegraNivelCritico.EhCritico(l.Nivel.Value)))).ToArray();
    }

    public Task<bool> DispositivoExisteAsync(int id, CancellationToken ct) =>
        db.Dispositivos.AsNoTracking().AnyAsync(d => d.Id == id, ct);

    public async Task<LeituraSensorResponse> RegistrarAsync(LeituraSensor leitura, CancellationToken ct)
    {
        db.LeiturasSensor.Add(leitura);
        await db.SaveChangesAsync(ct);
        return new(leitura.Id, leitura.DispositivoId, leitura.NivelPercentual,
            DateTime.SpecifyKind(leitura.LidaEm, DateTimeKind.Utc), RegraNivelCritico.EhCritico(leitura.NivelPercentual));
    }
}
