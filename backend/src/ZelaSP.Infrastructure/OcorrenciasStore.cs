using Microsoft.EntityFrameworkCore;
using ZelaSP.Application;
using ZelaSP.Domain;

namespace ZelaSP.Infrastructure;

public sealed class OcorrenciasStore(ZelaSpDbContext db) : IOcorrenciasStore
{
    public async Task<IReadOnlyList<BairroResponse>> ListarBairrosAsync(CancellationToken ct) =>
        await db.Bairros.AsNoTracking().OrderBy(x => x.Nome).ThenBy(x => x.Id)
            .Select(x => new BairroResponse(x.Id, x.Nome, x.Subprefeitura)).ToListAsync(ct);

    public async Task<IReadOnlyList<CategoriaOcorrenciaResponse>> ListarCategoriasAsync(CancellationToken ct) =>
        await db.CategoriasOcorrencia.AsNoTracking().OrderBy(x => x.Nome).ThenBy(x => x.Id)
            .Select(x => new CategoriaOcorrenciaResponse(x.Id, x.Nome)).ToListAsync(ct);

    public Task<bool> BairroExisteAsync(int id, CancellationToken ct) => db.Bairros.AsNoTracking().AnyAsync(x => x.Id == id, ct);
    public Task<bool> CategoriaExisteAsync(int id, CancellationToken ct) => db.CategoriasOcorrencia.AsNoTracking().AnyAsync(x => x.Id == id, ct);

    public async Task<OcorrenciaResponse?> ObterAsync(int id, CancellationToken ct)
    {
        var entity = await db.Ocorrencias.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return entity is null ? null : Map(entity);
    }

    public async Task<PaginaOcorrenciasResponse> ListarAsync(ListarOcorrenciasQuery query, CancellationToken ct)
    {
        var source = db.Ocorrencias.AsNoTracking();
        if (query.BairroId is { } bairro) source = source.Where(x => x.BairroId == bairro);
        if (query.CategoriaId is { } categoria) source = source.Where(x => x.CategoriaId == categoria);
        if (!string.IsNullOrEmpty(query.Status))
        {
            var status = Enum.Parse<StatusOcorrencia>(query.Status);
            source = source.Where(x => x.Status == status);
        }
        var total = await source.CountAsync(ct);
        var items = await source.OrderByDescending(x => x.CriadaEm).ThenByDescending(x => x.Id)
            .Skip((query.Pagina - 1) * query.TamanhoPagina).Take(query.TamanhoPagina).ToListAsync(ct);
        return new(items.Select(Map).ToArray(), query.Pagina, query.TamanhoPagina, total,
            (int)Math.Ceiling((double)total / query.TamanhoPagina));
    }

    public async Task<OcorrenciaResponse> CriarAsync(Ocorrencia ocorrencia, CancellationToken ct, IReadOnlyList<EventoSeguranca>? eventos = null)
    {
        db.Ocorrencias.Add(ocorrencia);
        if (eventos is not null) db.EventosSeguranca.AddRange(eventos);
        // One relational SaveChanges commits the occurrence and all audit events atomically.
        await db.SaveChangesAsync(ct);
        return Map(ocorrencia);
    }

    public async Task AlterarStatusComHistoricoAsync(int id, StatusOcorrencia anterior, StatusOcorrencia novo,
        DateTime alteradoEm, string executor, CancellationToken ct)
    {
        // Explicit transaction covers both the conditional UPDATE and history INSERT.
        // Comparing the previous state avoids duplicate histories and lost concurrent transitions.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var affected = await db.Ocorrencias.Where(x => x.Id == id && x.Status == anterior)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, novo), ct);
        if (affected != 1)
            throw new ConflitoEstadoException("A ocorrência foi alterada por outra solicitação. Consulte novamente.");

        db.HistoricosStatus.Add(new HistoricoStatus
        {
            OcorrenciaId = id, StatusAnterior = anterior, StatusNovo = novo,
            AlteradoEm = alteradoEm, Executor = executor
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        // Disposing an uncommitted transaction rolls back on exceptions, including cancellation.
    }

    public async Task<IReadOnlyList<HistoricoStatusResponse>> HistoricoAsync(int id, CancellationToken ct) =>
        await db.HistoricosStatus.AsNoTracking().Where(x => x.OcorrenciaId == id)
            .OrderBy(x => x.AlteradoEm).ThenBy(x => x.Id)
            .Select(x => new HistoricoStatusResponse(x.Id, x.StatusAnterior, x.StatusNovo,
                DateTime.SpecifyKind(x.AlteradoEm, DateTimeKind.Utc), x.Executor)).ToListAsync(ct);

    private static OcorrenciaResponse Map(Ocorrencia x) => new(x.Id, x.BairroId, x.CategoriaId,
        x.Descricao, x.Status, DateTime.SpecifyKind(x.CriadaEm, DateTimeKind.Utc), x.Origem);
}
