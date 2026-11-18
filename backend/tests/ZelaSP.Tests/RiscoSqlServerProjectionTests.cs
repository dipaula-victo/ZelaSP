using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Xunit;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

public sealed class RiscoSqlServerProjectionTests
{
    [Fact, Trait("Category", "SqlServerTranslation")]
    public async Task ConsultaRealGeraAgregacaoSqlServerSemAbrirConexao()
    {
        var capture = new Capture();
        var guard = new NoConnection();
        await using var db = new ZelaSpDbContext(new DbContextOptionsBuilder<ZelaSpDbContext>()
            .UseSqlServer("Server=.\\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true")
            .AddInterceptors(capture, guard).Options);
        await Assert.ThrowsAsync<Captured>(() => new RiscoCidadaoStore(db).ContarAsync(DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(7), default));
        capture.Enabled = false;
        var expression = new BindParameters().Visit(capture.Expression)!;
        var query = ((IQueryable)db.EventosSeguranca).Provider.CreateQuery(expression);
        var sql = query.ToQueryString();
        Assert.Contains("COUNT_BIG", sql);
        Assert.Contains("GROUP BY", sql);
        Assert.Contains("Latin1_General_100_BIN2", sql);
        Assert.Contains("DATALENGTH", sql);
        Assert.Contains("[ocorrido_em] >=", sql);
        Assert.Contains("[ocorrido_em] <=", sql);
        Assert.DoesNotContain("[origem]", sql);
        Assert.DoesNotContain("[destino]", sql);
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
    private sealed class BindParameters : ExpressionVisitor
    {
        protected override Expression VisitExtension(Expression node) => node is QueryParameterExpression p ? p.Name switch
        {
            "inicioUtc" => Expression.Constant(DateTime.UnixEpoch),
            "fimUtc" => Expression.Constant(DateTime.UnixEpoch.AddDays(7)),
            "sqlServer" => Expression.Constant(true),
            "Functions" => Expression.Constant(EF.Functions),
            _ => throw new InvalidOperationException("Parâmetro inesperado no diagnóstico offline: " + p.Name)
        } : base.VisitExtension(node);
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
