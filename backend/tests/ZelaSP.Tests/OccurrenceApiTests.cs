using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZelaSP.Application;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;
using Xunit;

namespace ZelaSP.Tests;

public sealed class OccurrenceApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    internal static async Task<OcorrenciaResponse> CreateAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/ocorrencias", new { bairroId = 1, categoriaId = 1, descricao = "Relato exclusivamente fictício." });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OcorrenciaResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task T01_ListarBairros()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        var list = await c.GetFromJsonAsync<BairroResponse[]>("/api/bairros");
        Assert.Equal("Bairro fictício", Assert.Single(list!).Nome);
    }

    [Fact]
    public async Task T02_ListarCategorias()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        Assert.Equal("Categoria fictícia", Assert.Single((await c.GetFromJsonAsync<CategoriaOcorrenciaResponse[]>("/api/categorias-ocorrencia"))!).Nome);
    }

    [Fact]
    public async Task ReferenciasVaziasRetornamListasVazias()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync(false);
        using var c = f.CreateClient();
        Assert.Empty((await c.GetFromJsonAsync<BairroResponse[]>("/api/bairros"))!);
        Assert.Empty((await c.GetFromJsonAsync<CategoriaOcorrenciaResponse[]>("/api/categorias-ocorrencia"))!);
        var page = await c.GetFromJsonAsync<PaginaOcorrenciasResponse>("/api/ocorrencias");
        Assert.Empty(page!.Itens);
        Assert.Equal(0, page.TotalRegistros);
        Assert.Equal(0, page.TotalPaginas);
    }

    [Fact]
    public async Task T03_CadastrarOcorrenciaPersisteERetornaLocation()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        using var response = await c.PostAsJsonAsync("/api/ocorrencias", new { bairroId = 1, categoriaId = 1, descricao = "  Relato fictício.  " });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = (await response.Content.ReadFromJsonAsync<OcorrenciaResponse>(JsonOptions))!;
        Assert.Equal($"http://localhost/api/ocorrencias/{item.Id}", response.Headers.Location!.ToString());
        Assert.Equal(StatusOcorrencia.ABERTA, item.Status);
        Assert.Equal(OrigemOcorrencia.APP, item.Origem);
        Assert.Equal(DateTimeKind.Utc, item.CriadaEm.Kind);
        Assert.Equal(new DateTime(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc), item.CriadaEm);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Equal("Relato fictício.", (await db.Ocorrencias.AsNoTracking().SingleAsync()).Descricao);
        Assert.Empty(await db.HistoricosStatus.ToListAsync());
    }

    [Theory]
    [InlineData(999, 1)] // T04
    [InlineData(1, 999)] // T05
    public async Task T04_T05_RejeitarReferenciasInexistentes(int bairroId, int categoriaId)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        using var response = await c.PostAsJsonAsync("/api/ocorrencias", new { bairroId, categoriaId, descricao = "Relato fictício." });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var scope = f.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>().Ocorrencias.ToListAsync());
    }

    [Fact]
    public async Task T06_ConsultarOcorrenciaExistente()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        var created = await CreateAsync(c);
        var found = await c.GetFromJsonAsync<OcorrenciaResponse>($"/api/ocorrencias/{created.Id}", JsonOptions);
        Assert.Equal(created, found);
    }

    [Theory]
    [InlineData("/api/ocorrencias/999")]
    [InlineData("/api/ocorrencias/999/historico")]
    public async Task T07_InexistenteRetorna404(string path)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PatchAsJsonAsync("/api/ocorrencias/999/status", new { status = "EM_ANALISE" })).StatusCode);
    }

    [Fact]
    public async Task T08_PaginacaoOrdenacaoEFiltros()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        var first = await CreateAsync(c);
        var second = await CreateAsync(c);
        await c.PatchAsJsonAsync($"/api/ocorrencias/{second.Id}/status", new { status = "EM_ANALISE" });
        var page = await c.GetFromJsonAsync<PaginaOcorrenciasResponse>("/api/ocorrencias?pagina=1&tamanhoPagina=1", JsonOptions);
        Assert.Equal(second.Id, Assert.Single(page!.Itens).Id);
        Assert.Equal(2, page.TotalRegistros);
        Assert.Equal(2, page.TotalPaginas);
        var next = await c.GetFromJsonAsync<PaginaOcorrenciasResponse>("/api/ocorrencias?pagina=2&tamanhoPagina=1", JsonOptions);
        Assert.Equal(first.Id, Assert.Single(next!.Itens).Id);
        var filtered = await c.GetFromJsonAsync<PaginaOcorrenciasResponse>("/api/ocorrencias?bairroId=1&categoriaId=1&status=EM_ANALISE", JsonOptions);
        Assert.Equal(second.Id, Assert.Single(filtered!.Itens).Id);
        var empty = await c.GetFromJsonAsync<PaginaOcorrenciasResponse>("/api/ocorrencias?bairroId=999", JsonOptions);
        Assert.Empty(empty!.Itens);
    }

    [Fact]
    public async Task T09_T11_T12_TransicoesValidasPersistemHistoricoConsistente()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        var item = await CreateAsync(c);
        Assert.Empty((await c.GetFromJsonAsync<HistoricoStatusResponse[]>($"/api/ocorrencias/{item.Id}/historico", JsonOptions))!);
        foreach (var status in new[] { "EM_ANALISE", "RESOLVIDA" })
            Assert.Equal(HttpStatusCode.OK, (await c.PatchAsJsonAsync($"/api/ocorrencias/{item.Id}/status", new { status })).StatusCode);
        var history = (await c.GetFromJsonAsync<HistoricoStatusResponse[]>($"/api/ocorrencias/{item.Id}/historico", JsonOptions))!;
        Assert.Equal(2, history.Length);
        Assert.Equal(StatusOcorrencia.ABERTA, history[0].StatusAnterior);
        Assert.Equal(StatusOcorrencia.EM_ANALISE, history[0].StatusNovo);
        Assert.Equal(history[0].StatusNovo, history[1].StatusAnterior);
        Assert.Equal(StatusOcorrencia.RESOLVIDA, history[1].StatusNovo);
        Assert.True(history[0].Id < history[1].Id); // Fixed clock also verifies the tie breaker.
        Assert.All(history, x => { Assert.Equal("zelasp-api", x.Executor); Assert.Equal(DateTimeKind.Utc, x.AlteradoEm.Kind); });
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Equal(StatusOcorrencia.RESOLVIDA, (await db.Ocorrencias.AsNoTracking().SingleAsync()).Status);
        Assert.Equal(2, await db.HistoricosStatus.CountAsync());
    }

    [Theory]
    [InlineData("{\"status\":\"INVALIDO\"}")]
    [InlineData("{\"status\":99}")]
    [InlineData("{\"status\":1}")]
    [InlineData("{\"status\":\"1\"}")]
    [InlineData("{}")]
    [InlineData("{\"status\":null}")]
    [InlineData("{\"status\":\"EM_ANALISE\",\"executor\":\"cliente\"}")]
    public async Task T10_StatusInvalidoRejeitado(string json)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        var item = await CreateAsync(c);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PatchAsync($"/api/ocorrencias/{item.Id}/status", content)).StatusCode);
    }

    [Fact]
    public async Task TransicoesProibidasRetornam409SemHistoricoAdicional()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        var item = await CreateAsync(c);
        foreach (var status in new[] { "ABERTA", "RESOLVIDA" })
            Assert.Equal(HttpStatusCode.Conflict, (await c.PatchAsJsonAsync($"/api/ocorrencias/{item.Id}/status", new { status })).StatusCode);
        await c.PatchAsJsonAsync($"/api/ocorrencias/{item.Id}/status", new { status = "EM_ANALISE" });
        foreach (var status in new[] { "ABERTA", "EM_ANALISE" })
            Assert.Equal(HttpStatusCode.Conflict, (await c.PatchAsJsonAsync($"/api/ocorrencias/{item.Id}/status", new { status })).StatusCode);
        await c.PatchAsJsonAsync($"/api/ocorrencias/{item.Id}/status", new { status = "RESOLVIDA" });
        foreach (var status in new[] { "ABERTA", "EM_ANALISE", "RESOLVIDA" })
            Assert.Equal(HttpStatusCode.Conflict, (await c.PatchAsJsonAsync($"/api/ocorrencias/{item.Id}/status", new { status })).StatusCode);
        Assert.Equal(2, (await c.GetFromJsonAsync<HistoricoStatusResponse[]>($"/api/ocorrencias/{item.Id}/historico", JsonOptions))!.Length);
    }

    [Fact]
    public async Task T13_FalhaDepoisDoUpdateReverteStatusENaoGravaHistorico()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        var item = await CreateAsync(c);
        f.Failure.FailHistory = true;
        using var response = await c.PatchAsJsonAsync($"/api/ocorrencias/{item.Id}/status", new { status = "EM_ANALISE" });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("INTERNAL_HISTORY_FAILURE_SENTINEL", body);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Equal(StatusOcorrencia.ABERTA, (await db.Ocorrencias.AsNoTracking().SingleAsync()).Status);
        Assert.Empty(await db.HistoricosStatus.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task EstadoAnteriorDesatualizadoNaoSobrescreveStatusNemDuplicaHistorico()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        var item = await CreateAsync(c);
        await c.PatchAsJsonAsync($"/api/ocorrencias/{item.Id}/status", new { status = "EM_ANALISE" });
        using var scope = f.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOcorrenciasStore>();
        await Assert.ThrowsAsync<ConflitoEstadoException>(() => store.AlterarStatusComHistoricoAsync(item.Id,
            StatusOcorrencia.ABERTA, StatusOcorrencia.EM_ANALISE, DateTime.UtcNow, "teste-tecnico", default));
        Assert.Single(await scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>().HistoricosStatus.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"bairroId\":0,\"categoriaId\":1,\"descricao\":\"Fictício\"}")]
    [InlineData("{\"bairroId\":1,\"categoriaId\":1,\"descricao\":\"   \"}")]
    [InlineData("{\"bairroId\":1,\"categoriaId\":1,\"descricao\":\"Fictício\",\"status\":\"RESOLVIDA\"}")]
    [InlineData("{\"bairroId\":1,\"categoriaId\":1,\"descricao\":\"Fictício\",\"origem\":\"SENSOR\"}")]
    [InlineData("{\"bairroId\":1,\"categoriaId\":1,\"descricao\":\"Fictício\",\"id\":7}")]
    public async Task ValidacaoPostRejeitaCamposInvalidosEControlados(string json)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        using var response = await c.PostAsync("/api/ocorrencias", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("ZelaSP.Application", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(4000, HttpStatusCode.Created)]
    [InlineData(4001, HttpStatusCode.BadRequest)]
    public async Task DescricaoRespeitaLimite(int length, HttpStatusCode expected)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        Assert.Equal(expected, (await c.PostAsJsonAsync("/api/ocorrencias", new { bairroId = 1, categoriaId = 1, descricao = new string('x', length) })).StatusCode);
    }

    [Theory]
    [InlineData("pagina=0")]
    [InlineData("tamanhoPagina=101")]
    [InlineData("tamanhoPagina=0")]
    [InlineData("status=99")]
    [InlineData("status=INVALIDO")]
    [InlineData("bairroId=-1")]
    [InlineData("categoriaId=0")]
    [InlineData("pagina=2147483647&tamanhoPagina=100")]
    public async Task ConsultaInvalidaRetorna400(string query)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/ocorrencias?" + query)).StatusCode);
    }

    [Fact]
    public async Task LeiturasNaoRastreiamEntidades()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        var item = await CreateAsync(c);
        using var scope = f.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<OcorrenciasService>();
        await service.ListarBairrosAsync(default);
        await service.ListarCategoriasAsync(default);
        await service.ObterAsync(item.Id, default);
        await service.ListarAsync(new(), default);
        await service.HistoricoAsync(item.Id, default);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>().ChangeTracker.Entries());
    }

    [Fact]
    public async Task OpenApiContemSeteOperacoesComContratosEExemplos()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var c = f.CreateClient();
        using var document = JsonDocument.Parse(await c.GetStringAsync("/openapi/v1.json"));
        var paths = document.RootElement.GetProperty("paths");
        var expected = new[] { ("/api/bairros", "get"), ("/api/categorias-ocorrencia", "get"),
            ("/api/ocorrencias", "post"), ("/api/ocorrencias", "get"), ("/api/ocorrencias/{id}", "get"),
            ("/api/ocorrencias/{id}/status", "patch"), ("/api/ocorrencias/{id}/historico", "get") };
        foreach (var (path, verb) in expected)
        {
            var op = paths.GetProperty(path).GetProperty(verb);
            Assert.False(string.IsNullOrWhiteSpace(op.GetProperty("summary").GetString()));
            Assert.True(op.GetProperty("responses").TryGetProperty("500", out _));
            Assert.True(op.GetProperty("responses").GetProperty("500").GetProperty("content").TryGetProperty("application/problem+json", out _));
            var response = op.GetProperty("responses").GetProperty(verb == "post" ? "201" : "200");
            Assert.True(response.GetProperty("content").GetProperty("application/json").TryGetProperty("example", out _));
            if (verb is "post" or "patch")
                Assert.True(op.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").TryGetProperty("example", out _));
        }
        var parameters = paths.GetProperty("/api/ocorrencias").GetProperty("get").GetProperty("parameters").EnumerateArray().ToArray();
        Assert.Contains(parameters, p => p.GetProperty("name").GetString() == "pagina");
        var statusParameter = parameters.Single(p => p.GetProperty("name").GetString() == "status");
        Assert.Equal(3, statusParameter.GetProperty("schema").GetProperty("enum").GetArrayLength());
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.Equal("string", schemas.GetProperty("StatusOcorrencia").GetProperty("type").GetString());
        Assert.Equal("string", schemas.GetProperty("OrigemOcorrencia").GetProperty("type").GetString());
        var createSchema = schemas.GetProperty("CriarOcorrenciaRequest");
        Assert.Equal(3, createSchema.GetProperty("required").GetArrayLength());
        Assert.False(createSchema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal("integer", createSchema.GetProperty("properties").GetProperty("bairroId").GetProperty("type").GetString());
        var patchSchema = schemas.GetProperty("AtualizarStatusRequest");
        Assert.False(patchSchema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal("string", patchSchema.GetProperty("properties").GetProperty("status").GetProperty("type").GetString());
    }
}
