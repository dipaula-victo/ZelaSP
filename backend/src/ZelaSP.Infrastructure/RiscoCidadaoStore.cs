using Microsoft.EntityFrameworkCore;
using ZelaSP.Application;
using ZelaSP.Domain;

namespace ZelaSP.Infrastructure;

public sealed class RiscoCidadaoStore(ZelaSpDbContext db) : IRiscoCidadaoStore
{
    public async Task<ContagensRisco> ContarAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken ct)
    {
        var sqlServer = db.Database.IsSqlServer();
        // Exact vocabulary even when the SQL Server database has a case-insensitive collation.
        var source = db.EventosSeguranca.AsNoTracking().Where(e => e.OcorridoEm >= inicioUtc && e.OcorridoEm <= fimUtc)
            .Select(e => new
            {
                Tipo = sqlServer ? EF.Functions.Collate(e.TipoEvento, "Latin1_General_100_BIN2") : e.TipoEvento,
                Categoria = sqlServer ? EF.Functions.Collate(e.TipoDado, "Latin1_General_100_BIN2") : e.TipoDado,
                TamanhoTipo = sqlServer ? EF.Functions.DataLength(e.TipoEvento) : e.TipoEvento.Length * 2,
                TamanhoCategoria = sqlServer ? EF.Functions.DataLength(e.TipoDado) : e.TipoDado.Length * 2
            })
            .Where(e => ((e.Tipo == "PII_DETECTADO" && e.TamanhoTipo == 26) || (e.Tipo == "VAZAMENTO_BLOQUEADO" && e.TamanhoTipo == 38)) &&
                ((e.Categoria == "CPF" && e.TamanhoCategoria == 6) || (e.Categoria == "TELEFONE" && e.TamanhoCategoria == 16) ||
                 (e.Categoria == "EMAIL" && e.TamanhoCategoria == 10) || (e.Categoria == "SENHA" && e.TamanhoCategoria == 10) ||
                 (e.Categoria == "SAUDE" && e.TamanhoCategoria == 10)));
        var grupos = await source.GroupBy(e => new { e.Tipo, e.Categoria })
            .Select(g => new { g.Key.Tipo, g.Key.Categoria, Quantidade = g.LongCount() }).ToListAsync(ct);
        long Count(string categoria) => grupos.Where(g => g.Tipo == "PII_DETECTADO" && g.Categoria == categoria).Sum(g => g.Quantidade);
        return new(Count("CPF"), Count("TELEFONE"), Count("EMAIL"), Count("SENHA"), Count("SAUDE"),
            grupos.Where(g => g.Tipo == "VAZAMENTO_BLOQUEADO").Sum(g => g.Quantidade));
    }
}
