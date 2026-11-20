using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;
using ZelaSP.Application;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

public sealed class TaintBarrierTests
{
    // Same fixtures as PiiApiTests; never sourced from a person's records.
    // Valid CPF check digits do not establish that a number is unassigned.
    private const string Original = "Relato fictício: CPF 12345678909, (11) 91234-0000, ficticio@example.test; senha=SomenteFicticia; diabetes.";
    private const string Protected = "Relato fictício: CPF [CPF OCULTADO], [TELEFONE OCULTADO], [E-MAIL OCULTADO]; senha=[SENHA OCULTADA]; [SAÚDE OCULTADA].";
    private static readonly string[] Values = ["12345678909", "91234-0000", "ficticio@example.test", "SomenteFicticia", "diabetes"];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task PiiSanitizadaAuditadaEAgregadaComSaidaContaminadaRejeitadaSemEscritasNoGet()
    {
        var guard = new ReadOnlyDuringGet();
        await using var factory = await OccurrenceApiFactory.CreateAsync(additionalInterceptor: guard);
        using var handler = new RiscoAiTests.Handler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(OpenAiExplicacaoCidadaClient.Endpoint, request.RequestUri);
            var body = await request.Content!.ReadAsStringAsync(ct);
            AssertNoOriginal(body);
            foreach (var forbidden in new[] { "descricao", "ocorrenciaId", "eventoId", "executor", "nomeArquivo", "rmLider", "Relato fict", "OCULTADO" })
                Assert.DoesNotContain(forbidden, body, StringComparison.OrdinalIgnoreCase);
            using var document = JsonDocument.Parse(body);
            using var input = JsonDocument.Parse(document.RootElement.GetProperty("input").GetString()!);
            var aggregate = input.RootElement;
            Assert.Equal(new[] { "deteccoes", "nivel", "periodoDias", "pontuacao", "vazamentosBloqueados" },
                aggregate.EnumerateObject().Select(p => p.Name).Order().ToArray());
            Assert.Equal(30, aggregate.GetProperty("pontuacao").GetInt32());
            Assert.Equal("MEDIO", aggregate.GetProperty("nivel").GetString());
            Assert.Equal(7, aggregate.GetProperty("periodoDias").GetInt32());
            Assert.Equal(0, aggregate.GetProperty("vazamentosBloqueados").GetInt64());
            var counts = aggregate.GetProperty("deteccoes");
            Assert.Equal(new[] { "cpf", "email", "saude", "senha", "telefone" }, counts.EnumerateObject().Select(p => p.Name).Order().ToArray());
            Assert.All(counts.EnumerateObject(), field => Assert.Equal(1, field.Value.GetInt64()));
            // Simulated provider output, not an outbound disclosure or a new audit event.
            return RiscoAiTests.Envelope("senha: SomenteFicticia");
        });
        using var http = new HttpClient(handler);
        var observed = new ObservedClient(new OpenAiExplicacaoCidadaClient(http, Options.Create(new RiscoAiOptions
        {
            Enabled = true, Provider = "OpenAI", Model = "gpt-4.1-mini", ApiKey = "VALOR_FICTICIO_DE_TESTE_NAO_E_CHAVE"
        })));
        await using var host = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IExplicacaoCidadaClient>();
            services.AddSingleton<IExplicacaoCidadaClient>(observed);
        }));
        using var client = host.CreateClient();
        using var post = await client.PostAsJsonAsync("/api/ocorrencias", new { bairroId = 1, categoriaId = 1, descricao = Original });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        AssertNoOriginal(await post.Content.ReadAsStringAsync());
        var created = (await post.Content.ReadFromJsonAsync<OcorrenciaResponse>(Json))!;
        Assert.Equal(Protected, created.Descricao);
        Assert.Equal(0, handler.Chamadas);

        string before;
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
            Assert.True(db.Database.IsSqlite());
            var occurrence = await db.Ocorrencias.AsNoTracking().SingleAsync();
            Assert.Equal(Protected, occurrence.Descricao);
            Assert.Equal(created.Id, occurrence.Id);
            var events = await db.EventosSeguranca.AsNoTracking().OrderBy(e => e.Id).ToArrayAsync();
            Assert.Equal(new[] { "CPF", "EMAIL", "SAUDE", "SENHA", "TELEFONE" }, events.Select(e => e.TipoDado).Order().ToArray());
            Assert.All(events, e =>
            {
                Assert.Equal("PII_DETECTADO", e.TipoEvento);
                Assert.Equal("MASCARADO", e.Acao);
                Assert.Equal("API_OCORRENCIAS", e.Origem);
                Assert.Equal("BANCO", e.Destino);
                Assert.Equal(occurrence.CriadaEm, e.OcorridoEm);
            });
            before = JsonSerializer.Serialize(new { occurrence, events }, Json);
            AssertNoOriginal(before);
            Assert.Empty(await db.HistoricosStatus.ToListAsync());
            Assert.Empty(await db.MensagensRisco.ToListAsync());
        }

        guard.Active = true;
        using var get = await client.GetAsync("/api/risco-cidadao");
        guard.Active = false;
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var publicBody = await get.Content.ReadAsStringAsync();
        AssertNoOriginal(publicBody);
        var risk = (await get.Content.ReadFromJsonAsync<RiscoCidadaoResponse>(Json))!;
        Assert.Equal("FALLBACK", risk.Explicacao.Origem);
        Assert.True(ValidadorExplicacaoCidada.Aceita(risk.Explicacao.Texto));
        Assert.Equal(30, risk.Pontuacao);
        Assert.Equal(NivelRisco.MEDIO, risk.Nivel);
        Assert.Equal(new DeteccoesRiscoResponse(1, 1, 1, 1, 1), risk.Deteccoes);
        Assert.Equal(0, risk.VazamentosBloqueados);
        Assert.Equal(1, observed.Calls);
        Assert.True(observed.ReturnedNull); // Real adapter rejected the contaminated output.
        Assert.Equal(1, handler.Chamadas);
        Assert.Single(guard.Commands);
        Assert.DoesNotContain("OCORRENCIA", guard.Commands[0], StringComparison.OrdinalIgnoreCase);

        using var verify = host.Services.CreateScope();
        var final = verify.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.True(final.Database.IsSqlite());
        var finalOccurrence = await final.Ocorrencias.AsNoTracking().SingleAsync();
        var finalEvents = await final.EventosSeguranca.AsNoTracking().OrderBy(e => e.Id).ToArrayAsync();
        Assert.Equal(before, JsonSerializer.Serialize(new { occurrence = finalOccurrence, events = finalEvents }, Json));
        Assert.DoesNotContain(finalEvents, e => e.TipoEvento == "VAZAMENTO_BLOQUEADO");
        Assert.Empty(await final.MensagensRisco.ToListAsync());
        Assert.Empty(await final.HistoricosStatus.ToListAsync());
    }

    private static void AssertNoOriginal(string text)
    {
        foreach (var value in Values) Assert.DoesNotContain(value, text, StringComparison.OrdinalIgnoreCase);
    }

    // Observation only: delegates to the real adapter and never substitutes its result.
    private sealed class ObservedClient(OpenAiExplicacaoCidadaClient inner) : IExplicacaoCidadaClient
    {
        public int Calls { get; private set; }
        public bool ReturnedNull { get; private set; }
        public async Task<string?> GerarAsync(ResumoRiscoAi resumo, CancellationToken ct)
        {
            Calls++;
            var result = await inner.GerarAsync(resumo, ct);
            ReturnedNull = result is null;
            return result;
        }
    }

    private sealed class ReadOnlyDuringGet : DbCommandInterceptor
    {
        public bool Active { get; set; }
        public List<string> Commands { get; } = [];
        private void Check(DbCommand command, CommandEventData data)
        {
            if (!Active) return;
            Assert.True(data.Context!.Database.IsSqlite());
            Assert.StartsWith("SELECT", command.CommandText.TrimStart(), StringComparison.OrdinalIgnoreCase);
            Commands.Add(command.CommandText);
        }
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result)
        { Check(command, data); return result; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result, CancellationToken ct = default)
        { Check(command, data); return ValueTask.FromResult(result); }
        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData data, InterceptionResult<object> result)
        { Check(command, data); return result; }
        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<object> result, CancellationToken ct = default)
        { Check(command, data); return ValueTask.FromResult(result); }
        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData data, InterceptionResult<int> result)
        { Check(command, data); return result; }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<int> result, CancellationToken ct = default)
        { Check(command, data); return ValueTask.FromResult(result); }
    }
}
