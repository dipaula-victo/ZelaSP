using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZelaSP.Application;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

public sealed class RiscoApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private static readonly DateTime Agora = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);
    private static EventoSeguranca Evento(string tipo, string categoria, DateTime? quando = null) => new()
    {
        TipoEvento = tipo, TipoDado = categoria, Origem = "SENTINELA_ORIGEM_FICTICIA", Destino = "SENTINELA_DESTINO_FICTICIO",
        Acao = "SENTINELA_ACAO_FICTICIA", OcorridoEm = quando ?? Agora
    };
    private static async Task Seed(OccurrenceApiFactory factory, params EventoSeguranca[] eventos)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.True(db.Database.IsSqlite());
        db.EventosSeguranca.AddRange(eventos);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task JanelaInclusivaIgnoraFuturosAntigosEVocabularioDesconhecido()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        await Seed(f, Evento("PII_DETECTADO", "CPF", Agora.AddDays(-7)), Evento("PII_DETECTADO", "CPF", Agora),
            Evento("PII_DETECTADO", "TELEFONE", Agora.AddDays(-7).AddMilliseconds(-1)),
            Evento("PII_DETECTADO", "EMAIL", Agora.AddMilliseconds(1)), Evento("DESCONHECIDO", "CPF"),
            Evento("PII_DETECTADO", "DESCONHECIDO"), Evento("VAZAMENTO_BLOQUEADO", "DESCONHECIDO"),
            Evento("pii_detectado", "CPF"), Evento("PII_DETECTADO", "cpf"), Evento("PII_DETECTADO ", "CPF"), Evento("PII_DETECTADO", "CPF "));
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        var contagens = await new RiscoCidadaoStore(db).ContarAsync(Agora.AddDays(-7), Agora, default);
        Assert.Equal(new ContagensRisco(2, 0, 0, 0, 0, 0), contagens);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task BancoVazioRetornaBaixoEFallbackSemGravar()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        await using var isolated = f.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(
            new Dictionary<string, string?> { ["RiscoAi:Enabled"] = "false" })));
        using var client = isolated.CreateClient();
        using var response = await client.GetAsync("/api/risco-cidadao");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<RiscoCidadaoResponse>(Json))!;
        Assert.Equal(0, result.Pontuacao);
        Assert.Equal(NivelRisco.BAIXO, result.Nivel);
        Assert.Equal("FALLBACK", result.Explicacao.Origem);
        Assert.Empty(result.Alertas);
        Assert.Equal(7, result.PeriodoDias);
        Assert.Equal(Agora, result.Janela.FimUtc);
        Assert.Equal(Agora.AddDays(-7), result.Janela.InicioUtc);
        Assert.Equal(DateTimeKind.Utc, result.Janela.FimUtc.Kind);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Empty(await db.EventosSeguranca.ToListAsync());
        Assert.Empty(await db.MensagensRisco.ToListAsync());
        Assert.Empty(await db.Ocorrencias.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContratoMobilePayloadSeguroEGetSemEscritasMesmoComLlm(bool falha)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        await Seed(f, Evento("PII_DETECTADO", "CPF"), Evento("PII_DETECTADO", "TELEFONE"), Evento("PII_DETECTADO", "EMAIL"),
            Evento("PII_DETECTADO", "SENHA"), Evento("PII_DETECTADO", "SAUDE"), Evento("VAZAMENTO_BLOQUEADO", "SENHA"));
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
            db.Ocorrencias.Add(new() { BairroId = 1, CategoriaId = 1, Descricao = "SENTINELA_RELATO senha=Ficticia123 ficticio@example.test 12345678909 diabetes",
                Status = StatusOcorrencia.ABERTA, Origem = OrigemOcorrencia.APP, CriadaEm = Agora });
            await db.SaveChangesAsync();
        }
        using var handler = new RiscoAiTests.Handler(async (request, ct) =>
        {
            var body = await request.Content!.ReadAsStringAsync(ct);
            foreach (var proibido in new[] { "SENTINELA", "Ficticia123", "ficticio@example.test", "12345678909", "diabetes", "554787", "executor", "descricao", "nomeArquivo", "ocorrenciaId" })
                Assert.DoesNotContain(proibido, body, StringComparison.OrdinalIgnoreCase);
            return falha ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : RiscoAiTests.Envelope(RiscoAiTests.TextoSeguro);
        });
        var somenteLeitura = new SomenteLeitura();
        await using var host = f.WithWebHostBuilder(b =>
        {
            b.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RiscoAi:Enabled"] = "true", ["RiscoAi:ApiKey"] = "VALOR_FICTICIO_DE_TESTE_NAO_E_CHAVE", ["RiscoAi:Provider"] = "OpenAI"
            }));
            b.ConfigureServices(services =>
            {
                services.AddHttpClient<IExplicacaoCidadaClient, OpenAiExplicacaoCidadaClient>().ConfigurePrimaryHttpMessageHandler(() => handler);
                services.AddDbContext<ZelaSpDbContext>(o => o.AddInterceptors(somenteLeitura));
            });
        });
        using var client = host.CreateClient();
        using var response = await client.GetAsync("/api/risco-cidadao");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<RiscoCidadaoResponse>(Json))!;
        Assert.Equal(45, result.Pontuacao); // 6+3+3+10+8+15, no extra SENHA for the blocked event.
        Assert.Equal(NivelRisco.MEDIO, result.Nivel);
        Assert.Equal(new DeteccoesRiscoResponse(1, 1, 1, 1, 1), result.Deteccoes);
        Assert.Equal(1, result.VazamentosBloqueados);
        Assert.Equal(falha ? "FALLBACK" : "LLM", result.Explicacao.Origem);
        Assert.Equal(1, handler.Chamadas);
        Assert.Single(somenteLeitura.Comandos);
        Assert.DoesNotContain("OCORRENCIA", somenteLeitura.Comandos[0]);
        Assert.DoesNotContain("origem", somenteLeitura.Comandos[0]);
        Assert.DoesNotContain("destino", somenteLeitura.Comandos[0]);
        // Compare to the actual Mobile constants, without writing or running Mobile tools.
        var mobileFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../mobile/src/services/privacy.ts"));
        var mobile = await File.ReadAllTextAsync(mobileFile);
        Assert.Equal(new[] { "CPF", "TELEFONE", "EMAIL", "SENHA", "SAUDE", "VAZAMENTO" }, result.Alertas.Select(a => a.Tipo));
        Assert.All(result.Alertas, a => Assert.Contains(JsonSerializer.Serialize(a.Texto, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }), mobile));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal(7, root.GetProperty("periodoDias").GetInt32());
        Assert.Equal("MEDIO", root.GetProperty("nivel").GetString());
        Assert.True(root.TryGetProperty("explicacao", out _));
        foreach (var k in new[] { "cpf", "telefone", "email", "senha", "saude" }) Assert.Equal(1, root.GetProperty("deteccoes").GetProperty(k).GetInt32());
        using var verify = f.Services.CreateScope();
        var context = verify.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Equal(6, await context.EventosSeguranca.CountAsync());
        Assert.Empty(await context.MensagensRisco.ToListAsync());
        Assert.Equal(1, await context.Ocorrencias.CountAsync());
    }

    [Fact]
    public async Task OpenApiDocumentaRiscoSemExporConfiguracao()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        var text = await client.GetStringAsync("/openapi/v1.json");
        using var json = JsonDocument.Parse(text);
        var operation = json.RootElement.GetProperty("paths").GetProperty("/api/risco-cidadao").GetProperty("get");
        Assert.Contains("sete dias", operation.GetProperty("description").GetString());
        Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _));
        Assert.True(operation.GetProperty("responses").TryGetProperty("500", out _));
        Assert.DoesNotContain("ApiKey", text);
    }

    private sealed class SomenteLeitura : DbCommandInterceptor
    {
        public List<string> Comandos { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Assert.StartsWith("SELECT", command.CommandText.TrimStart());
            Comandos.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) => throw new InvalidOperationException("GET não pode gravar");
    }
}
