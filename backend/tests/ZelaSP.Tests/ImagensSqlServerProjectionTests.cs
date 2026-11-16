using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Xunit;
using Xunit.Abstractions;
using ZelaSP.Application;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

public sealed class ImagensSqlServerProjectionTests(ITestOutputHelper output)
{
    [Fact, Trait("Category", "SqlServerTranslation")]
    public async Task ProjecaoRealDeMetadadosTraduzTamanhoNoSqlServerSemAbrirConexao()
    {
        var capture = new QueryCapture();
        var guard = new NoConnection();
        await using var db = new ZelaSpDbContext(new DbContextOptionsBuilder<ZelaSpDbContext>()
            .UseSqlServer("Server=.\\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true")
            .AddInterceptors(capture, guard).Options);

        // Capture the expression from the actual store, not a manually copied projection.
        await Assert.ThrowsAsync<QueryCapturedException>(() => new ImagensStore(db).ObterMetadadosAsync(int.MaxValue, default));
        var terminal = Assert.IsAssignableFrom<MethodCallExpression>(capture.Expression);
        Assert.Equal(nameof(Queryable.SingleOrDefault), terminal.Method.Name);
        capture.Enabled = false;
        // Compilation interceptors see EF's already-extracted scalar parameter. Bind only
        // this diagnostic ID so ToQueryString has no external parameter bag to resolve.
        var expression = new DiagnosticId().Visit(terminal.Arguments[0])!;
        var source = ((IQueryable<ImagemOcorrencia>)db.ImagensOcorrencia).Provider
            .CreateQuery<ImagemMetadadosResponse>(expression);
        var sql = source.ToQueryString(); // SQL generation only: never enumerate or execute.
        output.WriteLine(sql);
        Assert.Contains("DATALENGTH(", sql);
        Assert.Contains("CAST(DATALENGTH([i].[dados]) AS int)", sql);
        Assert.DoesNotContain("SELECT [i].[dados]", sql);
        Assert.Equal(0, guard.Attempts);
        output.WriteLine("Conexões abertas: 0. Expressão real do ImagensStore; CAST de DATALENGTH para int confirmado.");
    }

    private sealed class QueryCapturedException : Exception;

    private sealed class DiagnosticId : ExpressionVisitor
    {
        protected override Expression VisitExtension(Expression node) => node is QueryParameterExpression { Name: "id" }
            ? Expression.Constant(int.MaxValue) : base.VisitExtension(node);
    }

    private sealed class QueryCapture : IQueryExpressionInterceptor
    {
        public bool Enabled { get; set; } = true;
        public Expression? Expression { get; private set; }
        public Expression QueryCompilationStarting(Expression expression, QueryExpressionEventData eventData)
        {
            if (!Enabled) return expression;
            Expression = expression;
            throw new QueryCapturedException();
        }
    }

    private sealed class NoConnection : DbConnectionInterceptor
    {
        public int Attempts { get; private set; }
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            Attempts++;
            throw new InvalidOperationException("Diagnóstico offline não pode abrir conexão.");
        }
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            Attempts++;
            throw new InvalidOperationException("Diagnóstico offline não pode abrir conexão.");
        }
    }
}
