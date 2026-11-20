using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using ZelaSP.Application;
using ZelaSP.Domain;

namespace ZelaSP.Tests;

// TestServer only. Replace both external boundaries; no database/provider calls.
public sealed class RiskCorsTests
{
    private static WebApplicationFactory<Program> Factory(string environment) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment)
                .UseSetting("ConnectionStrings:ZelaSP", "Server=127.0.0.1,1;Database=NotUsed;Integrated Security=true;Connect Timeout=1")
                .UseSetting("RiscoAi:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IRiscoCidadaoStore>();
                services.RemoveAll<IExplicacaoCidadaClient>();
                services.AddSingleton<IRiscoCidadaoStore, EmptyStore>();
                services.AddSingleton<IExplicacaoCidadaClient, NoAi>();
            });
        });

    [Theory]
    [InlineData("Development", "http://127.0.0.1:8081", true)]
    [InlineData("Development", "http://localhost:8081", false)]
    [InlineData("Development", "http://127.0.0.1:8082", false)]
    [InlineData("Development", "https://127.0.0.1:8081", false)]
    [InlineData("Development", "https://example.test", false)]
    [InlineData("Production", "http://127.0.0.1:8081", false)]
    public async Task OnlyExactDevelopmentOriginCanReadRisk(string environment, string origin, bool allowed)
    {
        await using var factory = Factory(environment);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/risco-cidadao");
        request.Headers.Add("Origin", origin);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed) Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.Contains("FALLBACK", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/openapi/v1.json", "GET", null, false)]
    [InlineData("/api/risco-cidadao/", "GET", null, false)]
    [InlineData("/api/risco-cidadao", "POST", null, false)]
    [InlineData("/api/risco-cidadao", "OPTIONS", "POST", false)]
    [InlineData("/api/risco-cidadao", "OPTIONS", "GET", true)]
    [InlineData("/api/bairros", "OPTIONS", "GET", false)]
    public async Task CorsDoesNotExtendToOtherRoutesOrMethods(string path, string method, string? preflight, bool allowed)
    {
        await using var factory = Factory("Development");
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Add("Origin", "http://127.0.0.1:8081");
        if (preflight is not null) request.Headers.Add("Access-Control-Request-Method", preflight);
        using var response = await client.SendAsync(request);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
        if (allowed)
        {
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("GET", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Methods")));
            Assert.False(response.Headers.Contains("Access-Control-Allow-Headers"));
        }
    }

    private sealed class EmptyStore : IRiscoCidadaoStore
    {
        public Task<ContagensRisco> ContarAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken ct) =>
            Task.FromResult(new ContagensRisco(0, 0, 0, 0, 0, 0));
    }
    private sealed class NoAi : IExplicacaoCidadaClient
    {
        public Task<string?> GerarAsync(ResumoRiscoAi resumo, CancellationToken ct) => Task.FromResult<string?>(null);
    }
}
