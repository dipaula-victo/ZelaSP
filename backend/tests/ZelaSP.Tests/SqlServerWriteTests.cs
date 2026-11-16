using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using ZelaSP.Application;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

// This attribute prevents opening a SQL connection during ordinary test runs.
public sealed class SqlServerWriteFactAttribute : FactAttribute
{
    public SqlServerWriteFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION") != "PM_PO_TASK002_APROVADO")
            Skip = "Escrita no SQL Server depende de autorização específica do PM/PO. Ver docs/SQLSERVER_INTEGRATION.md.";
    }
}

public sealed class SqlServerWriteTests(ITestOutputHelper output)
{
    private const string ConnectionString = "Server=.\\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true";

    [SqlServerWriteFact, Trait("Category", "SqlServerWrite")]
    public async Task PersistenciaHistoricoERollbackNoSqlServerSomenteAposAutorizacao()
    {
        // Recheck authorization at execution time, before any connection or write.
        Assert.Equal("PM_PO_TASK002_APROVADO", Environment.GetEnvironmentVariable("ZELASP_SQLSERVER_TASK002_WRITE_AUTHORIZATION"));
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT CASE WHEN
                    CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))=N'slayr\SQLEXPRESS'
                    AND CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName'))=N'SQLEXPRESS'
                    AND DB_NAME()=N'ZelaSP_Dev'
                    AND DATABASEPROPERTYEX(DB_NAME(),'Status')=N'ONLINE'
                    THEN 1 ELSE 0 END;
                """;
            Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));
        }

        DbContextOptions<ZelaSpDbContext> Options(IInterceptor? interceptor = null)
        {
            var builder = new DbContextOptionsBuilder<ZelaSpDbContext>().UseSqlServer(connection);
            if (interceptor is not null) builder.AddInterceptors(interceptor);
            return builder.Options;
        }

        var marker = "TASK002-FICTICIO-" + Guid.NewGuid().ToString("N");
        output.WriteLine("Execução fictícia identificada por: " + marker);
        int bairroId, categoriaId, ocorrenciaId;
        await using (var setup = new ZelaSpDbContext(Options()))
        {
            Assert.Equal(["20261114191732_InitialCreate"], await setup.Database.GetAppliedMigrationsAsync());
            Assert.False(setup.Database.HasPendingModelChanges());
            output.WriteLine($"Estado anterior: bairros={await setup.Bairros.CountAsync()}, categorias={await setup.CategoriasOcorrencia.CountAsync()}, ocorrencias={await setup.Ocorrencias.CountAsync()}, historicos={await setup.HistoricosStatus.CountAsync()}.");
            // Only new marker-owned fictional references; never reuse/update existing records.
            var bairro = new Bairro { Nome = marker, Subprefeitura = "Subprefeitura exclusivamente fictícia" };
            var categoria = new CategoriaOcorrencia { Nome = marker };
            setup.AddRange(bairro, categoria);
            await setup.SaveChangesAsync();
            bairroId = bairro.Id;
            categoriaId = categoria.Id;
        }

        await using (var db = new ZelaSpDbContext(Options()))
        {
            var service = new OcorrenciasService(new OcorrenciasStore(db), TimeProvider.System, new("integracao-task002"));
            var created = await service.CriarAsync(new() { BairroId = bairroId, CategoriaId = categoriaId, Descricao = marker }, default);
            ocorrenciaId = created.Id;
            Assert.Equal(StatusOcorrencia.ABERTA, created.Status);
        }
        await using (var db = new ZelaSpDbContext(Options()))
        {
            var service = new OcorrenciasService(new OcorrenciasStore(db), TimeProvider.System, new("integracao-task002"));
            await service.AtualizarStatusAsync(ocorrenciaId, new() { Status = StatusOcorrencia.EM_ANALISE }, default);
        }
        await using (var db = new ZelaSpDbContext(Options()))
        {
            var item = await db.Ocorrencias.AsNoTracking().SingleAsync(x => x.Id == ocorrenciaId);
            Assert.Equal(marker, item.Descricao);
            Assert.Equal(StatusOcorrencia.EM_ANALISE, item.Status);
            var history = await db.HistoricosStatus.AsNoTracking().SingleAsync(x => x.OcorrenciaId == ocorrenciaId);
            Assert.Equal(StatusOcorrencia.ABERTA, history.StatusAnterior);
            Assert.Equal(StatusOcorrencia.EM_ANALISE, history.StatusNovo);
        }
        var failure = new HistoryFailureInterceptor { FailHistory = true };
        await using (var db = new ZelaSpDbContext(Options(failure)))
        {
            var service = new OcorrenciasService(new OcorrenciasStore(db), TimeProvider.System, new("integracao-task002"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AtualizarStatusAsync(ocorrenciaId,
                new() { Status = StatusOcorrencia.RESOLVIDA }, default));
        }
        await using (var verify = new ZelaSpDbContext(Options()))
        {
            Assert.Equal(StatusOcorrencia.EM_ANALISE,
                (await verify.Ocorrencias.AsNoTracking().SingleAsync(x => x.Id == ocorrenciaId)).Status);
            Assert.Equal(1, await verify.HistoricosStatus.CountAsync(x => x.OcorrenciaId == ocorrenciaId));
        }
        output.WriteLine($"Registros fictícios mantidos, sem limpeza: bairroId={bairroId}, categoriaId={categoriaId}, ocorrenciaId={ocorrenciaId}. Histórico final: 1 alteração confirmada; segunda alteração revertida.");
    }
}
