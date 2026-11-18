using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ZelaSP.Application;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

public sealed class PiiApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private const string Original = "Relato fictício: CPF 12345678909, (11) 91234-0000, ficticio@example.test; senha=SomenteFicticia; diabetes.";
    private const string Protegido = "Relato fictício: CPF [CPF OCULTADO], [TELEFONE OCULTADO], [E-MAIL OCULTADO]; senha=[SENHA OCULTADA]; [SAÚDE OCULTADA].";
    private static Task<HttpResponseMessage> Post(HttpClient client, string texto) =>
        client.PostAsJsonAsync("/api/ocorrencias", new { bairroId = 1, categoriaId = 1, descricao = texto });

    [Fact]
    public async Task PostGravaSomenteSanitizadoComEventosTecnicosELeiturasProtegidas()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        using var response = await Post(client, Original);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = (await response.Content.ReadFromJsonAsync<OcorrenciaResponse>(Json))!;
        Assert.Equal(Protegido, item.Descricao);
        Assert.Equal($"/api/ocorrencias/{item.Id}", response.Headers.Location!.AbsolutePath);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.True(db.Database.IsSqlite());
        var saved = await db.Ocorrencias.AsNoTracking().SingleAsync();
        Assert.Equal(Protegido, saved.Descricao);
        Assert.Equal(StatusOcorrencia.ABERTA, saved.Status);
        Assert.Equal(OrigemOcorrencia.APP, saved.Origem);
        var events = await db.EventosSeguranca.AsNoTracking().ToListAsync();
        Assert.Equal(5, events.Count);
        Assert.Equal(5, events.Select(x => x.TipoDado).Distinct().Count());
        Assert.All(events, e =>
        {
            Assert.Equal("PII_DETECTADO", e.TipoEvento);
            Assert.Equal("API_OCORRENCIAS", e.Origem);
            Assert.Equal("BANCO", e.Destino);
            Assert.Equal("MASCARADO", e.Acao);
            Assert.Equal(saved.CriadaEm, e.OcorridoEm);
        });
        Assert.Empty(await db.HistoricosStatus.ToListAsync());
        Assert.Empty(await db.MensagensRisco.ToListAsync());
        Assert.Equal(Protegido, (await client.GetFromJsonAsync<OcorrenciaResponse>($"/api/ocorrencias/{item.Id}", Json))!.Descricao);
        Assert.Equal(Protegido, Assert.Single((await client.GetFromJsonAsync<PaginaOcorrenciasResponse>("/api/ocorrencias", Json))!.Itens).Descricao);
        using var patch = await client.PatchAsJsonAsync($"/api/ocorrencias/{item.Id}/status", new { status = "EM_ANALISE" });
        Assert.Equal(Protegido, (await patch.Content.ReadFromJsonAsync<OcorrenciaResponse>(Json))!.Descricao);
        Assert.Equal(5, await db.EventosSeguranca.CountAsync());
    }

    [Fact]
    public async Task LeituraLegadaMascaraSemRegravarOuGerarEventos()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        var legacy = new Ocorrencia { BairroId = 1, CategoriaId = 1, Descricao = Original,
            CriadaEm = DateTime.UtcNow, Status = StatusOcorrencia.ABERTA, Origem = OrigemOcorrencia.APP };
        db.Ocorrencias.Add(legacy);
        await db.SaveChangesAsync();
        Assert.Equal(Protegido, (await client.GetFromJsonAsync<OcorrenciaResponse>($"/api/ocorrencias/{legacy.Id}", Json))!.Descricao);
        Assert.Equal(Protegido, Assert.Single((await client.GetFromJsonAsync<PaginaOcorrenciasResponse>("/api/ocorrencias", Json))!.Itens).Descricao);
        Assert.Equal(Original, (await db.Ocorrencias.AsNoTracking().SingleAsync()).Descricao);
        Assert.Empty(await db.EventosSeguranca.ToListAsync());
    }

    [Theory]
    [InlineData("Relato sem dados sensíveis.")]
    [InlineData("[CPF OCULTADO] senha: [SENHA OCULTADA]")]
    public async Task SemDeteccoesNaoCriaEventos(string texto)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        using var response = await Post(client, texto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(texto, (await response.Content.ReadFromJsonAsync<OcorrenciaResponse>(Json))!.Descricao);
        using var scope = f.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>().EventosSeguranca.ToListAsync());
    }

    [Theory]
    [InlineData(3986, HttpStatusCode.Created)] // 3986 + 11 input = 3997; protected = 4000.
    [InlineData(3987, HttpStatusCode.BadRequest)]
    [InlineData(3989, HttpStatusCode.BadRequest)] // Input exactly 4000; output would exceed schema.
    public async Task LimiteConsideraTextoOriginalESanitizado(int prefixo, HttpStatusCode esperado)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        using var response = await Post(client, new string('x', prefixo - 1) + " " + "12345678909");
        Assert.Equal(esperado, response.StatusCode);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Equal(esperado == HttpStatusCode.Created ? 1 : 0, await db.Ocorrencias.CountAsync());
        Assert.Equal(esperado == HttpStatusCode.Created ? 1 : 0, await db.EventosSeguranca.CountAsync());
    }

    [Fact]
    public async Task FalhaRealDepoisDoPrimeiroInsertReverteOcorrenciaEEventosSemExporLogs()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        var failure = new SecondInsertFailure();
        var logs = new CapturedLogs();
        await using var failing = f.WithWebHostBuilder(b =>
        {
            b.ConfigureLogging(logging => logging.AddProvider(logs));
            b.ConfigureServices(s => s.AddDbContext<ZelaSpDbContext>(o => o.AddInterceptors(failure)));
        });
        using var client = failing.CreateClient();
        using var response = await Post(client, Original);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(2, failure.Inserts);
        Assert.True(failure.FirstInsertRead);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SomenteFicticia", body);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SomenteFicticia", string.Join("\n", logs.Messages));
        Assert.DoesNotContain("12345678909", string.Join("\n", logs.Messages));
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Empty(await db.Ocorrencias.AsNoTracking().ToListAsync());
        Assert.Empty(await db.EventosSeguranca.AsNoTracking().ToListAsync());
        Assert.Equal(1, await db.Bairros.CountAsync());
        Assert.Equal(1, await db.CategoriasOcorrencia.CountAsync());
    }

    [Theory]
    [InlineData("{\"bairroId\":\"ficticio@example.test\",\"categoriaId\":1,\"descricao\":\"senha=SomenteFicticia\"}")]
    [InlineData("{\"bairroId\":1,\"categoriaId\":1,\"descricao\":\"senha=SomenteFicticia\",\"ficticio@example.test\":1}")]
    public async Task ErrosDeBindingNaoEcoamValoresNemNomesDeCamposDoCliente(string json)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        using var response = await client.PostAsync("/api/ocorrencias", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("ficticio@example.test", body);
        Assert.DoesNotContain("SomenteFicticia", body);
        using var scope = f.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>().Ocorrencias.ToListAsync());
    }

    [Fact]
    public async Task RepeticoesAuditamCadaTrechoSemPersistirValoresOuPosicoes()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        var logs = new CapturedLogs();
        await using var observed = f.WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddProvider(logs)));
        using var client = observed.CreateClient();
        using var response = await Post(client, "12345678909 e 123.456.789-09; senha: ficticio@example.test");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = (await response.Content.ReadFromJsonAsync<OcorrenciaResponse>(Json))!;
        Assert.Equal("[CPF OCULTADO] e [CPF OCULTADO]; senha: [SENHA OCULTADA]", item.Descricao);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        var eventos = await db.EventosSeguranca.AsNoTracking().ToListAsync();
        Assert.Equal(3, eventos.Count);
        Assert.Equal(2, eventos.Count(e => e.TipoDado == "CPF"));
        Assert.Single(eventos, e => e.TipoDado == "SENHA");
        var serialized = JsonSerializer.Serialize(eventos, Json);
        Assert.DoesNotContain("12345678909", serialized);
        Assert.DoesNotContain("123.456.789-09", serialized);
        Assert.DoesNotContain("ficticio@example.test", serialized);
        var captured = string.Join("\n", logs.Messages);
        Assert.DoesNotContain("12345678909", captured);
        Assert.DoesNotContain("ficticio@example.test", captured);
    }

    [Theory]
    [InlineData("senha: minha senha ficticia", "senha: [SENHA OCULTADA]", 1)]
    [InlineData("senha=minha senha 12345678909 ficticio@example.test", "senha=[SENHA OCULTADA]", 1)]
    [InlineData("senha é primeira ficticia; senha: segunda ficticia", "senha é [SENHA OCULTADA]; senha: [SENHA OCULTADA]", 2)]
    [InlineData("senha: minha senha ficticia\nRelato público", "senha: [SENHA OCULTADA]\nRelato público", 1)]
    public async Task SenhaInteiraProtegidaNoHttpENoBanco(string original, string esperado, int eventos)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        using var response = await Post(client, original);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = (await response.Content.ReadFromJsonAsync<OcorrenciaResponse>(Json))!;
        Assert.Equal(esperado, item.Descricao);
        Assert.Equal(esperado, (await client.GetFromJsonAsync<OcorrenciaResponse>($"/api/ocorrencias/{item.Id}", Json))!.Descricao);
        Assert.Equal(esperado, Assert.Single((await client.GetFromJsonAsync<PaginaOcorrenciasResponse>("/api/ocorrencias", Json))!.Itens).Descricao);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Equal(esperado, (await db.Ocorrencias.AsNoTracking().SingleAsync()).Descricao);
        var auditoria = await db.EventosSeguranca.AsNoTracking().ToListAsync();
        Assert.Equal(eventos, auditoria.Count);
        Assert.All(auditoria, e => Assert.Equal("SENHA", e.TipoDado));
        Assert.Empty(await db.HistoricosStatus.ToListAsync());
    }

    [Theory]
    [InlineData("senha: [CPF OCULTADO] SegredoFicticio123", "senha: [SENHA OCULTADA]", 1)]
    [InlineData("senha: [SENHA OCULTADA] SegredoFicticio123; público", "senha: [SENHA OCULTADA]; público", 1)]
    [InlineData("senha: [CPF OCULTADO]; público", "senha: [CPF OCULTADO]; público", 0)]
    [InlineData("senha: [SENHA OCULTADA]", "senha: [SENHA OCULTADA]", 0)]
    [InlineData("senha: \"[CPF OCULTADO] SegredoFicticio123\" público", "senha: \"[SENHA OCULTADA]\" público", 1)]
    [InlineData("senha: '[SENHA OCULTADA]' público", "senha: '[SENHA OCULTADA]' público", 0)]
    public async Task RotuloSoEhProtegidoQuandoOcupaValorInteiro(string original, string esperado, int eventos)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        using var response = await Post(client, original);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.DoesNotContain("SegredoFicticio123", await response.Content.ReadAsStringAsync());
        var item = (await response.Content.ReadFromJsonAsync<OcorrenciaResponse>(Json))!;
        Assert.Equal(esperado, item.Descricao);
        for (var i = 0; i < 3; i++)
        {
            var novamente = ProtecaoPii.Analisar(item.Descricao);
            Assert.Equal(esperado, novamente.Texto);
            Assert.Empty(novamente.Deteccoes);
        }
        Assert.Equal(esperado, (await client.GetFromJsonAsync<OcorrenciaResponse>($"/api/ocorrencias/{item.Id}", Json))!.Descricao);
        Assert.Equal(esperado, Assert.Single((await client.GetFromJsonAsync<PaginaOcorrenciasResponse>("/api/ocorrencias", Json))!.Itens).Descricao);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        var salvo = await db.Ocorrencias.AsNoTracking().SingleAsync();
        Assert.Equal(esperado, salvo.Descricao);
        Assert.DoesNotContain("SegredoFicticio123", salvo.Descricao);
        var auditoria = await db.EventosSeguranca.AsNoTracking().ToListAsync();
        Assert.Equal(eventos, auditoria.Count);
        Assert.All(auditoria, e => Assert.Equal("SENHA", e.TipoDado));
        Assert.DoesNotContain("SegredoFicticio123", JsonSerializer.Serialize(auditoria, Json));
    }

    private sealed class SecondInsertFailure : DbCommandInterceptor
    {
        public int Inserts { get; private set; }
        public bool FirstInsertRead { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) && ++Inserts == 2)
                throw new InvalidOperationException("senha=SomenteFicticia CPF 12345678909");
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (Inserts == 1) FirstInsertRead = true;
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class CapturedLogs : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();
        public ILogger CreateLogger(string categoryName) => new Capture(Messages);
        public void Dispose() { }
        private sealed class Capture(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                messages.Enqueue(formatter(state, exception) + exception?.ToString());
        }
    }
}
