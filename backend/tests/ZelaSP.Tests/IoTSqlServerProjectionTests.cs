using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

public sealed class IoTSqlServerProjectionTests
{
    [Fact, Trait("Category", "SqlServerTranslation")]
    public async Task ConsultaRealTraduzUltimaLeituraSemAbrirConexao()
    {
        var capture = new Capture();
        var guard = new NoConnection();
        await using var db = new ZelaSpDbContext(new DbContextOptionsBuilder<ZelaSpDbContext>()
            .UseSqlServer("Server=.\\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true")
            .AddInterceptors(capture, guard).Options);
        await Assert.ThrowsAsync<Captured>(() => new IoTStore(db).ListarDispositivosAsync(default));
        capture.Enabled = false;
        var sql = ((IQueryable)db.Dispositivos).Provider.CreateQuery(capture.Expression!).ToQueryString();
        Assert.Contains("LEFT JOIN", sql);
        Assert.Contains("TOP(1)", sql);
        Assert.Contains("[lida_em] DESC", sql);
        Assert.Contains("[id] DESC", sql);
        Assert.Contains("[nome]", sql);
        Assert.DoesNotContain("APPLY", sql);
        Assert.DoesNotContain("OCORRENCIA", sql);
        Assert.Equal(0, guard.Attempts);
    }
    private sealed class Captured : Exception;
    private sealed class Capture : IQueryExpressionInterceptor
    {
        public bool Enabled { get; set; } = true;
        public Expression? Expression { get; private set; }
        public Expression QueryCompilationStarting(Expression expression, QueryExpressionEventData eventData)
        {
            if (!Enabled) return expression;
            Expression = expression;
            throw new Captured();
        }
    }
    private sealed class NoConnection : DbConnectionInterceptor
    {
        public int Attempts { get; private set; }
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        { Attempts++; throw new InvalidOperationException("Diagnóstico offline não pode conectar."); }
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        { Attempts++; throw new InvalidOperationException("Diagnóstico offline não pode conectar."); }
    }
}
