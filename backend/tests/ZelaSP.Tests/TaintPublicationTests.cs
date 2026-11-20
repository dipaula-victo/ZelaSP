using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using ZelaSP.Application;
using ZelaSP.Domain;

namespace ZelaSP.Tests;

public sealed class TaintPublicationTests
{
    [Theory]
    [InlineData("CPF 12345678909", false)]
    [InlineData("Telefone (11) 91234-0000", false)]
    [InlineData("ficticio@example.test", false)]
    [InlineData("senha: Segredo Ficticio", false)]
    [InlineData("Informacao sobre diabetes", false)]
    [InlineData("<script>texto</script>", false)]
    [InlineData(null, false)]
    [InlineData("Evite informar dados pessoais nos relatos.", true)]
    public async Task ServicoRealPublicaSomenteTextoLiberadoOuFallback(string? input, bool accepted)
    {
        await using var factory = await OccurrenceApiFactory.CreateAsync(seedReferences: false);
        var source = new UntrustedClient(input);
        await using var host = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IExplicacaoCidadaClient>();
            services.AddSingleton<IExplicacaoCidadaClient>(source);
        }));
        using var client = host.CreateClient();
        using var response = await client.GetAsync("/api/risco-cidadao");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var explanation = json.RootElement.GetProperty("explicacao");
        var published = explanation.GetProperty("texto").GetString();
        Assert.Equal(accepted ? "LLM" : "FALLBACK", explanation.GetProperty("origem").GetString());
        Assert.True(TextoClassificado.Classificar(published).TentarPublicar(out var released));
        Assert.Equal(released, published);
        if (accepted) Assert.Equal(input, published);
        else if (input is not null) Assert.DoesNotContain(input, body);
        Assert.Equal(0, json.RootElement.GetProperty("pontuacao").GetInt32());
        Assert.Equal("BAIXO", json.RootElement.GetProperty("nivel").GetString());
        Assert.Equal(1, source.Calls);
    }

    // Simulates an untrusted client at the Application boundary, not an inference.
    // The real adapter's earlier barrier remains covered by RiscoAiTests/TaintBarrierTests.
    private sealed class UntrustedClient(string? text) : IExplicacaoCidadaClient
    {
        public int Calls { get; private set; }
        public Task<string?> GerarAsync(ResumoRiscoAi resumo, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            return Task.FromResult(text);
        }
    }
}
