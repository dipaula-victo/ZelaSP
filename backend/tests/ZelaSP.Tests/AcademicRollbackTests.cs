using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

public sealed class AcademicRollbackTests(ITestOutputHelper output)
{
    [Fact]
    public async Task T08_HttpFailureAfterBothWritesRollsBackStatusAndHistory()
    {
        var probe = new AfterWritesFailure();
        await using var factory = await OccurrenceApiFactory.CreateAsync(additionalInterceptor: probe);
        using var client = factory.CreateClient();
        var created = await OccurrenceApiTests.CreateAsync(client);
        using (var before = factory.Services.CreateScope())
        {
            var db = before.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
            Assert.True(db.Database.IsSqlite());
            Assert.Equal(StatusOcorrencia.ABERTA, (await db.Ocorrencias.AsNoTracking().SingleAsync()).Status);
            Assert.Equal(0, await db.HistoricosStatus.CountAsync());
        }
        probe.OccurrenceId = created.Id;
        probe.Armed = true;
        using var response = await client.PatchAsJsonAsync($"/api/ocorrencias/{created.Id}/status",
            new { status = "EM_ANALISE" });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain(AfterWritesFailure.Sentinel, await response.Content.ReadAsStringAsync());
        Assert.Equal(1, probe.Hits);
        Assert.True(probe.ActiveTransaction);
        Assert.Equal(StatusOcorrencia.EM_ANALISE, probe.StatusInsideTransaction);
        Assert.Equal(1, probe.HistoriesInsideTransaction);
        using var after = factory.Services.CreateScope();
        var verify = after.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Equal(StatusOcorrencia.ABERTA,
            (await verify.Ocorrencias.AsNoTracking().SingleAsync(x => x.Id == created.Id)).Status);
        Assert.Equal(0, await verify.HistoricosStatus.CountAsync());
        Assert.Equal(1, await verify.Ocorrencias.CountAsync());
        using var detail = await client.GetAsync($"/api/ocorrencias/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Contains("ABERTA", await detail.Content.ReadAsStringAsync());
        output.WriteLine($"SQLite temporario; ocorrenciaId={created.Id}; antes=ABERTA/0 historicos.");
        output.WriteLine($"PATCH /api/ocorrencias/{created.Id}/status {{\"status\":\"EM_ANALISE\"}} -> HTTP 500 Problem Details.");
        output.WriteLine("SavedChangesAsync: transacao ativa; SELECT real viu EM_ANALISE e 1 historico ABERTA->EM_ANALISE apos INSERT, antes de CommitAsync.");
        output.WriteLine("Novo DbContext apos falha: ABERTA/0 historicos; GET 200 ABERTA. Ambas as escritas revertidas.");
    }

    [Fact]
    public async Task CommitRemainsSuccessfulWhenTestFailureIsNotArmed()
    {
        var probe = new AfterWritesFailure();
        await using var factory = await OccurrenceApiFactory.CreateAsync(additionalInterceptor: probe);
        using var client = factory.CreateClient();
        var created = await OccurrenceApiTests.CreateAsync(client);
        using var response = await client.PatchAsJsonAsync($"/api/ocorrencias/{created.Id}/status",
            new { status = "EM_ANALISE" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Equal(StatusOcorrencia.EM_ANALISE, (await db.Ocorrencias.AsNoTracking().SingleAsync()).Status);
        var history = await db.HistoricosStatus.AsNoTracking().SingleAsync();
        Assert.Equal(StatusOcorrencia.ABERTA, history.StatusAnterior);
        Assert.Equal(StatusOcorrencia.EM_ANALISE, history.StatusNovo);
        Assert.Equal(0, probe.Hits);
    }

    private sealed class AfterWritesFailure : SaveChangesInterceptor
    {
        public const string Sentinel = "ACADEMIC_AFTER_BOTH_WRITES_FAILURE";
        public bool Armed { get; set; }
        public int OccurrenceId { get; set; }
        public int Hits { get; private set; }
        public bool ActiveTransaction { get; private set; }
        public StatusOcorrencia? StatusInsideTransaction { get; private set; }
        public int HistoriesInsideTransaction { get; private set; }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData,
            int result, CancellationToken cancellationToken = default)
        {
            if (!Armed) return result;
            var db = (ZelaSpDbContext)eventData.Context!;
            Assert.True(db.Database.IsSqlite());
            Assert.NotNull(db.Database.CurrentTransaction);
            ActiveTransaction = true;
            // SQL queries in the same active transaction prove persisted writes, not tracked values.
            StatusInsideTransaction = await db.Ocorrencias.AsNoTracking().Where(x => x.Id == OccurrenceId)
                .Select(x => x.Status).SingleAsync(cancellationToken);
            var histories = await db.HistoricosStatus.AsNoTracking().Where(x => x.OcorrenciaId == OccurrenceId)
                .ToListAsync(cancellationToken);
            HistoriesInsideTransaction = histories.Count;
            var history = Assert.Single(histories);
            Assert.Equal(StatusOcorrencia.ABERTA, history.StatusAnterior);
            Assert.Equal(StatusOcorrencia.EM_ANALISE, history.StatusNovo);
            Assert.Equal(1, result);
            Hits++;
            throw new InvalidOperationException(Sentinel);
        }
    }
}
