using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZelaSP.Infrastructure;
using Xunit;

namespace ZelaSP.Tests;

public sealed class ApiTests
{
    private static WebApplicationFactory<Program> CreateFactory(string environment) => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(b => b.UseEnvironment(environment).UseSetting("ConnectionStrings:ZelaSP",
            "Server=.\\SQLEXPRESS;Database=ZelaSP_Dev;Integrated Security=true;Encrypt=true;TrustServerCertificate=true"));

    [Fact]
    public async Task DevelopmentStartsResolvesContextAndServesOpenApiWithoutDatabaseAccess()
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("openapi", out _));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", db.Database.ProviderName);
    }

    [Fact]
    public async Task UnknownRouteReturnsProblemDetails()
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/nonexistent");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task OpenApiIsNotExposedInProduction()
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
    }
}
