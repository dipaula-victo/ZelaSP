using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using ZelaSP.Application;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

public sealed class OccurrenceConcurrencyTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DuasOperacoesQueLeramAbertaConfirmamUmaTransicaoEUmHistorico()
    {
        // Keep every temporary artifact inside backend's test output. No SQL Server access.
        var directory = Path.Combine(AppContext.BaseDirectory, "concurrency-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var databaseFile = Path.Combine(directory, "temporary.sqlite");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databaseFile, Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private, Pooling = false, DefaultTimeout = 5, ForeignKeys = true
        }.ToString();
        try
        {
            await RunScenarioAsync(connectionString);
        }
        finally
        {
            // Delete only this test's own files; never clean bin/obj or existing artifacts.
            foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" }) File.Delete(databaseFile + suffix);
            Directory.Delete(directory); // Non-recursive, so unexpected contents cannot be removed.
        }
    }

    private async Task RunScenarioAsync(string connectionString)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var ct = timeout.Token;
        int id;
        await using (var setupConnection = new SqliteConnection(connectionString))
        {
            await setupConnection.OpenAsync(ct);
            await using (var wal = setupConnection.CreateCommand())
            {
                wal.CommandText = "PRAGMA journal_mode=WAL;";
                Assert.Equal("wal", (string?)await wal.ExecuteScalarAsync(ct));
            }
            await using var setup = new ZelaSpDbContext(Options(setupConnection));
            Assert.True(setup.Database.IsSqlite());
            // Same fixture-only compatibility adaptation as the other relational tests.
            await setup.Database.ExecuteSqlRawAsync(setup.Database.GenerateCreateScript()
                .Replace("varbinary(max)", "BLOB", StringComparison.Ordinal), ct);
            var bairro = new Bairro { Nome = "Bairro fictício concorrência", Subprefeitura = "Subprefeitura fictícia" };
            var categoria = new CategoriaOcorrencia { Nome = "Categoria fictícia concorrência" };
            setup.AddRange(bairro, categoria);
            await setup.SaveChangesAsync(ct);
            var service = new OcorrenciasService(new OcorrenciasStore(setup), TimeProvider.System, new("setup-tecnico"));
            id = (await service.CriarAsync(new()
            {
                BairroId = bairro.Id, CategoriaId = categoria.Id, Descricao = "Ocorrência exclusivamente fictícia para concorrência."
            }, ct)).Id;
        }

        await using var connectionA = new SqliteConnection(connectionString);
        await using var connectionB = new SqliteConnection(connectionString);
        await connectionA.OpenAsync(ct);
        await connectionB.OpenAsync(ct);
        var observerA = new UpdateObserver();
        var observerB = new UpdateObserver();
        await using var dbA = new ZelaSpDbContext(Options(connectionA, observerA));
        await using var dbB = new ZelaSpDbContext(Options(connectionB, observerB));
        Assert.NotSame(dbA, dbB);
        Assert.NotSame(dbA.Database.GetDbConnection(), dbB.Database.GetDbConnection());
        var rendezvous = new ReadRendezvous();
        var serviceA = new OcorrenciasService(new ReadBarrierStore(new OcorrenciasStore(dbA), rendezvous, "A"),
            TimeProvider.System, new("concorrencia-A"));
        var serviceB = new OcorrenciasService(new ReadBarrierStore(new OcorrenciasStore(dbB), rendezvous, "B"),
            TimeProvider.System, new("concorrencia-B"));

        // SQLite's async methods can perform synchronous native work. Separate Task.Run
        // operations let the other writer progress while its peer waits for the database lock.
        var operationA = Task.Run(() => AttemptAsync("A", serviceA, id, ct), ct);
        var operationB = Task.Run(() => AttemptAsync("B", serviceB, id, ct), ct);
        var results = await Task.WhenAll(operationA, operationB).WaitAsync(TimeSpan.FromSeconds(25));

        Assert.Equal(2, rendezvous.Observed.Count);
        Assert.All(rendezvous.Observed.Values, x => { Assert.Equal(id, x.Id); Assert.Equal(StatusOcorrencia.ABERTA, x.Status); });
        var winner = Assert.Single(results, x => x.Response is not null);
        var loser = Assert.Single(results, x => x.Error is not null);
        Assert.Equal(StatusOcorrencia.EM_ANALISE, winner.Response!.Status);
        var conflict = Assert.IsType<ConflitoEstadoException>(loser.Error);
        Assert.Equal("A ocorrência foi alterada por outra solicitação. Consulte novamente.", conflict.Message);
        // Observe the real provider results; do not synthesize a conflict or catch BUSY as success.
        Assert.Equal([0, 1], observerA.AffectedRows.Concat(observerB.AffectedRows).Order().ToArray());
        Assert.Equal(1, Assert.Single(winner.Name == "A" ? observerA.AffectedRows : observerB.AffectedRows));
        Assert.Equal(0, Assert.Single(loser.Name == "A" ? observerA.AffectedRows : observerB.AffectedRows));

        await using var verificationConnection = new SqliteConnection(connectionString);
        await verificationConnection.OpenAsync(ct);
        await using var verify = new ZelaSpDbContext(Options(verificationConnection));
        var occurrence = Assert.Single(await verify.Ocorrencias.AsNoTracking().ToListAsync(ct));
        Assert.Equal(id, occurrence.Id);
        Assert.Equal(StatusOcorrencia.EM_ANALISE, occurrence.Status);
        var history = Assert.Single(await verify.HistoricosStatus.AsNoTracking().ToListAsync(ct));
        Assert.Equal(id, history.OcorrenciaId);
        Assert.Equal(StatusOcorrencia.ABERTA, history.StatusAnterior);
        Assert.Equal(StatusOcorrencia.EM_ANALISE, history.StatusNovo);
        Assert.Equal("concorrencia-" + winner.Name, history.Executor);
        Assert.Equal(1, await verify.Bairros.CountAsync(ct));
        Assert.Equal(1, await verify.CategoriasOcorrencia.CountAsync(ct));

        output.WriteLine("Conexões/DbContexts independentes: A e B. Barreira liberada somente após duas leituras reais ABERTA.");
        foreach (var result in results)
            output.WriteLine($"Operação {result.Name}: leu {rendezvous.Observed[result.Name].Status}; " +
                (result.Response is not null ? "confirmada; UPDATE afetou 1 linha." : "ConflitoEstadoException; UPDATE afetou 0 linhas."));
        output.WriteLine($"Conexão independente de verificação: status={occurrence.Status}, históricos=1, " +
            $"{history.StatusAnterior} → {history.StatusNovo}, executor={history.Executor}. Nenhum histórico duplicado ou alteração parcial.");
    }

    private static DbContextOptions<ZelaSpDbContext> Options(SqliteConnection connection, IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<ZelaSpDbContext>().UseSqlite(connection, o => o.CommandTimeout(5));
        if (interceptor is not null) builder.AddInterceptors(interceptor);
        return builder.Options;
    }

    private static async Task<Attempt> AttemptAsync(string name, OcorrenciasService service, int id, CancellationToken ct)
    {
        try { return new(name, await service.AtualizarStatusAsync(id, new() { Status = StatusOcorrencia.EM_ANALISE }, ct), null); }
        catch (Exception error) { return new(name, null, error); } // Every unexpected exception fails the assertions above.
    }

    private sealed record Attempt(string Name, OcorrenciaResponse? Response, Exception? Error);

    private sealed class ReadRendezvous
    {
        private readonly TaskCompletionSource _bothRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentDictionary<string, OcorrenciaResponse> Observed { get; } = new();

        public async Task ArriveAsync(string operation, OcorrenciaResponse? actual, CancellationToken ct)
        {
            Assert.NotNull(actual);
            Assert.Equal(StatusOcorrencia.ABERTA, actual.Status);
            Assert.True(Observed.TryAdd(operation, actual));
            if (Observed.Count == 2) _bothRead.TrySetResult();
            await _bothRead.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        }
    }

    // Synchronization decorator only: all values, SQL writes and exceptions come from
    // the production store. No mocks, predefined return values or ordering of winners.
    private sealed class ReadBarrierStore(IOcorrenciasStore actual, ReadRendezvous rendezvous, string name) : IOcorrenciasStore
    {
        public async Task<OcorrenciaResponse?> ObterAsync(int id, CancellationToken ct)
        {
            var response = await actual.ObterAsync(id, ct); // Fully materialized; reader already disposed.
            await rendezvous.ArriveAsync(name, response, ct);
            return response;
        }
        public Task<IReadOnlyList<BairroResponse>> ListarBairrosAsync(CancellationToken ct) => actual.ListarBairrosAsync(ct);
        public Task<IReadOnlyList<CategoriaOcorrenciaResponse>> ListarCategoriasAsync(CancellationToken ct) => actual.ListarCategoriasAsync(ct);
        public Task<bool> BairroExisteAsync(int id, CancellationToken ct) => actual.BairroExisteAsync(id, ct);
        public Task<bool> CategoriaExisteAsync(int id, CancellationToken ct) => actual.CategoriaExisteAsync(id, ct);
        public Task<PaginaOcorrenciasResponse> ListarAsync(ListarOcorrenciasQuery query, CancellationToken ct) => actual.ListarAsync(query, ct);
        public Task<OcorrenciaResponse> CriarAsync(Ocorrencia occurrence, CancellationToken ct, IReadOnlyList<EventoSeguranca>? eventos = null) => actual.CriarAsync(occurrence, ct, eventos);
        public Task AlterarStatusComHistoricoAsync(int id, StatusOcorrencia previous, StatusOcorrencia next,
            DateTime changed, string executor, CancellationToken ct) => actual.AlterarStatusComHistoricoAsync(id, previous, next, changed, executor, ct);
        public Task<IReadOnlyList<HistoricoStatusResponse>> HistoricoAsync(int id, CancellationToken ct) => actual.HistoricoAsync(id, ct);
    }

    private sealed class UpdateObserver : DbCommandInterceptor
    {
        public ConcurrentQueue<int> AffectedRows { get; } = new();
        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            int result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
                AffectedRows.Enqueue(result);
            return ValueTask.FromResult(result);
        }
    }
}
