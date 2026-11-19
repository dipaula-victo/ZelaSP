using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

internal sealed class OccurrenceApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public HistoryFailureInterceptor Failure { get; } = new();
    private IInterceptor? _additionalInterceptor;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders().AddConsole());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ZelaSpDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ZelaSpDbContext>>();
            services.RemoveAll<ZelaSpDbContext>();
            services.AddDbContext<ZelaSpDbContext>(o =>
            {
                o.UseSqlite(_connection).AddInterceptors(Failure);
                if (_additionalInterceptor is not null) o.AddInterceptors(_additionalInterceptor);
            });
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedClock());
        });
    }

    public static async Task<OccurrenceApiFactory> CreateAsync(bool seedReferences = true, IInterceptor? additionalInterceptor = null)
    {
        var factory = new OccurrenceApiFactory { _additionalInterceptor = additionalInterceptor };
        await factory._connection.OpenAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        if (!db.Database.IsSqlite()) throw new InvalidOperationException("Only temporary SQLite is permitted here.");
        // SQLite cannot parse SQL Server's varbinary(max). Adapt only this unused image
        // column in the ephemeral fixture DDL; the production model/migration stay untouched.
        var ddl = db.Database.GenerateCreateScript().Replace("varbinary(max)", "BLOB", StringComparison.Ordinal)
            // SQLite maps decimal to TEXT by default; give this temporary column numeric
            // affinity so the existing 0..100 CHECK compares numbers, not strings.
            .Replace("\"nivel_percentual\" TEXT", "\"nivel_percentual\" NUMERIC", StringComparison.Ordinal);
        await db.Database.ExecuteSqlRawAsync(ddl);
        if (seedReferences)
        {
            db.Bairros.Add(new Bairro { Nome = "Bairro fictício", Subprefeitura = "Subprefeitura fictícia" });
            db.CategoriasOcorrencia.Add(new CategoriaOcorrencia { Nome = "Categoria fictícia" });
            await db.SaveChangesAsync();
        }
        return factory;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
    }
}

internal sealed class HistoryFailureInterceptor : SaveChangesInterceptor
{
    public bool FailHistory { get; set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (FailHistory && eventData.Context!.ChangeTracker.Entries<HistoricoStatus>().Any(e => e.State == EntityState.Added))
            throw new InvalidOperationException("INTERNAL_HISTORY_FAILURE_SENTINEL");
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
