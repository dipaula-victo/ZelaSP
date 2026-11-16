using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZelaSP.Application;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

public sealed class ImagensApiTests
{
    private static string Executor(OccurrenceApiFactory factory, int index = 0) =>
        factory.Services.GetRequiredService<EquipeAcademica>().Executores[index];

    private static MultipartFormDataContent Form(string? id, string? title, string? executor,
        byte[]? bytes, string filename = "pixel-ficticio.png", string mime = "image/png", bool fileFirst = false)
    {
        var form = new MultipartFormDataContent();
        void FilePart()
        {
            if (bytes is null) return;
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue(mime);
            form.Add(file, "arquivo", filename);
        }
        if (fileFirst) FilePart();
        if (id is not null) form.Add(new StringContent(id), "ocorrenciaId");
        if (title is not null) form.Add(new StringContent(title), "titulo");
        if (executor is not null) form.Add(new StringContent(executor), "executor");
        if (!fileFirst) FilePart();
        return form;
    }

    [Theory]
    [InlineData("png", 0)]
    [InlineData("jpg", 1)]
    [InlineData("jpeg", 2)]
    [InlineData("png", 3)]
    public async Task UploadValidoPersisteMetadadosEBytesOriginaisSemMudarOcorrencia(string extension, int member)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        var occurrence = await OccurrenceApiTests.CreateAsync(client);
        await client.PatchAsJsonAsync($"/api/ocorrencias/{occurrence.Id}/status", new { status = "EM_ANALISE" });
        var bytes = extension == "png" ? ImagemFixtures.Png : ImagemFixtures.Jpeg;
        var mime = extension == "png" ? "image/png" : "image/jpeg";
        using var form = Form(occurrence.Id.ToString(), "  Pixel fictício  ", Executor(f, member), bytes, "pixel-ficticio." + extension, mime, fileFirst: true);
        using var response = await client.PostAsync("/api/imagens", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        var metadata = (await response.Content.ReadFromJsonAsync<ImagemMetadadosResponse>())!;
        Assert.Equal($"http://localhost/api/imagens/{metadata.Id}", response.Headers.Location!.ToString());
        Assert.Equal(occurrence.Id, metadata.OcorrenciaId);
        Assert.Equal("Pixel fictício", metadata.Titulo);
        Assert.Equal("pixel-ficticio." + extension, metadata.NomeArquivo);
        Assert.Equal(mime, metadata.Mime);
        Assert.Equal(bytes.Length, metadata.TamanhoBytes);
        Assert.Equal("554787", metadata.RmLider);
        Assert.Equal(Executor(f, member), metadata.Executor);
        Assert.Equal(DateTimeKind.Utc, metadata.DataUtc.Kind);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.TryGetProperty("dados", out _));
        Assert.DoesNotContain(Convert.ToBase64String(bytes), json);
        Assert.Equal(metadata, await client.GetFromJsonAsync<ImagemMetadadosResponse>($"/api/imagens/{metadata.Id}"));

        using var content = await client.GetAsync($"/api/imagens/{metadata.Id}/conteudo");
        Assert.Equal(HttpStatusCode.OK, content.StatusCode);
        Assert.Equal(mime, content.Content.Headers.ContentType!.MediaType);
        Assert.Equal(bytes, await content.Content.ReadAsByteArrayAsync());
        Assert.Equal("nosniff", Assert.Single(content.Headers.GetValues("X-Content-Type-Options")));

        // Independent DbContext, separate from HTTP request scopes; no mocked persistence.
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        var saved = Assert.Single(await db.ImagensOcorrencia.AsNoTracking().ToListAsync());
        Assert.Equal(bytes, saved.Dados);
        Assert.Equal(metadata.Id, saved.Id);
        Assert.Equal(metadata.RmLider, saved.RmLider);
        Assert.Equal(metadata.Executor, saved.Executor);
        var unchanged = await db.Ocorrencias.AsNoTracking().SingleAsync();
        Assert.Equal(StatusOcorrencia.EM_ANALISE, unchanged.Status);
        Assert.Equal(occurrence.Descricao, unchanged.Descricao);
        Assert.Equal(occurrence.BairroId, unchanged.BairroId);
        Assert.Equal(occurrence.CategoriaId, unchanged.CategoriaId);
        Assert.Equal(occurrence.CriadaEm, unchanged.CriadaEm);
        var history = Assert.Single(await db.HistoricosStatus.AsNoTracking().ToListAsync());
        Assert.Equal(StatusOcorrencia.ABERTA, history.StatusAnterior);
        Assert.Equal(StatusOcorrencia.EM_ANALISE, history.StatusNovo);
    }

    [Theory]
    [InlineData("titulo-ausente")]
    [InlineData("titulo-vazio")]
    [InlineData("titulo-espacos")]
    [InlineData("titulo-longo")]
    [InlineData("executor-ausente")]
    [InlineData("executor-invalido")]
    [InlineData("executor-case")]
    [InlineData("executor-espacos")]
    [InlineData("id-ausente")]
    [InlineData("id-zero")]
    [InlineData("id-negativo")]
    [InlineData("id-texto")]
    [InlineData("ocorrencia-inexistente")]
    [InlineData("arquivo-ausente")]
    [InlineData("arquivo-vazio")]
    [InlineData("arquivo-grande")]
    [InlineData("gif")]
    [InlineData("svg")]
    [InlineData("pdf")]
    [InlineData("zip")]
    [InlineData("sem-extensao")]
    [InlineData("mime-invalido")]
    [InlineData("assinatura-invalida")]
    [InlineData("png-com-jpeg")]
    [InlineData("jpeg-com-png")]
    [InlineData("mime-divergente")]
    [InlineData("nome-longo")]
    [InlineData("nome-invalido")]
    [InlineData("rm-cliente")]
    [InlineData("campo-desconhecido")]
    [InlineData("campo-duplicado")]
    [InlineData("arquivo-duplicado")]
    public async Task UploadInvalidoNaoGravaNemAlteraEstado(string scenario)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        var occurrence = await OccurrenceApiTests.CreateAsync(client);
        await client.PatchAsJsonAsync($"/api/ocorrencias/{occurrence.Id}/status", new { status = "EM_ANALISE" });
        string? id = occurrence.Id.ToString(), title = "Fictícia", executor = Executor(f);
        byte[]? bytes = ImagemFixtures.Png;
        var filename = "ficticia.png";
        var mime = "image/png";
        var expected = HttpStatusCode.BadRequest;
        switch (scenario)
        {
            case "titulo-ausente": title = null; break;
            case "titulo-vazio": title = ""; break;
            case "titulo-espacos": title = "   "; break;
            case "titulo-longo": title = new string('t', 101); break;
            case "executor-ausente": executor = null; break;
            case "executor-invalido": executor = "NaoIntegrante_RM000000"; break;
            case "executor-case": executor = executor!.ToLowerInvariant(); break;
            case "executor-espacos": executor = " " + executor + " "; break;
            case "id-ausente": id = null; break;
            case "id-zero": id = "0"; break;
            case "id-negativo": id = "-1"; break;
            case "id-texto": id = "abc"; break;
            case "ocorrencia-inexistente": id = "999"; expected = HttpStatusCode.NotFound; break;
            case "arquivo-ausente": bytes = null; break;
            case "arquivo-vazio": bytes = []; break;
            case "arquivo-grande": bytes = new byte[LimitesImagem.MaximoBytes + 1]; expected = HttpStatusCode.RequestEntityTooLarge; break;
            case "gif": filename = "ficticia.gif"; mime = "image/gif"; bytes = "GIF89a"u8.ToArray(); break;
            case "svg": filename = "ficticia.svg"; mime = "image/svg+xml"; bytes = "<svg/>"u8.ToArray(); break;
            case "pdf": filename = "ficticia.pdf"; mime = "application/pdf"; bytes = "%PDF"u8.ToArray(); break;
            case "zip": filename = "ficticia.zip"; mime = "application/zip"; bytes = [80,75,3,4]; break;
            case "sem-extensao": filename = "ficticia"; break;
            case "mime-invalido": mime = "application/octet-stream"; break;
            case "assinatura-invalida": bytes = "assinatura ficticia invalida"u8.ToArray(); break;
            case "png-com-jpeg": bytes = ImagemFixtures.Jpeg; break;
            case "jpeg-com-png": filename = "ficticia.jpeg"; mime = "image/jpeg"; break;
            case "mime-divergente": mime = "image/jpeg"; break;
            case "nome-longo": filename = new string('a', 252) + ".png"; break;
            case "nome-invalido": filename = "invalido?.png"; break;
        }
        using var form = Form(id, title, executor, bytes, filename, mime);
        if (scenario == "rm-cliente") form.Add(new StringContent("000000"), "rmLider");
        if (scenario == "campo-desconhecido") form.Add(new StringContent("x"), "qualquer");
        if (scenario == "campo-duplicado") form.Add(new StringContent("outro"), "titulo");
        if (scenario == "arquivo-duplicado") form.Add(new ByteArrayContent(ImagemFixtures.Png), "arquivo", "outro.png");
        using var response = await client.PostAsync("/api/imagens", form);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Empty(await db.ImagensOcorrencia.AsNoTracking().ToListAsync());
        var unchanged = Assert.Single(await db.Ocorrencias.AsNoTracking().ToListAsync());
        Assert.Equal(occurrence.Id, unchanged.Id);
        Assert.Equal(StatusOcorrencia.EM_ANALISE, unchanged.Status);
        Assert.Equal(occurrence.Descricao, unchanged.Descricao);
        var history = Assert.Single(await db.HistoricosStatus.AsNoTracking().ToListAsync());
        Assert.Equal(StatusOcorrencia.ABERTA, history.StatusAnterior);
        Assert.Equal(StatusOcorrencia.EM_ANALISE, history.StatusNovo);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task TituloValidaLimiteDepoisDeTrim(int length)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        var occurrence = await OccurrenceApiTests.CreateAsync(client);
        using var form = Form(occurrence.Id.ToString(), "  " + new string('x', length) + "  ", Executor(f), ImagemFixtures.Png);
        using var response = await client.PostAsync("/api/imagens", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(length, (await response.Content.ReadFromJsonAsync<ImagemMetadadosResponse>())!.Titulo.Length);
    }

    [Fact]
    public async Task TamanhoExatamenteDoisMiBAceitoSemDeclararIntegridadeCompleta()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        var occurrence = await OccurrenceApiTests.CreateAsync(client);
        // Synthetic signature-bearing payload for the byte boundary; not a decoder integrity test.
        var payload = new byte[LimitesImagem.MaximoBytes];
        ImagemFixtures.Png.CopyTo(payload, 0);
        using var form = Form(occurrence.Id.ToString(), "Limite fictício", Executor(f), payload);
        using var response = await client.PostAsync("/api/imagens", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var metadata = (await response.Content.ReadFromJsonAsync<ImagemMetadadosResponse>())!;
        Assert.Equal(LimitesImagem.MaximoBytes, metadata.TamanhoBytes);
        Assert.Equal(payload, await client.GetByteArrayAsync($"/api/imagens/{metadata.Id}/conteudo"));
    }

    [Theory]
    [InlineData("../../pixel-ficticio.PNG", "pixel-ficticio.PNG")]
    [InlineData("C:\\fakepath\\pixel-ficticio.png", "pixel-ficticio.png")]
    [InlineData("e\u0301-ficticio.png", "é-ficticio.png")]
    public async Task NomeNormalizadoSomenteComoMetadado(string original, string expected)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        var occurrence = await OccurrenceApiTests.CreateAsync(client);
        using var form = Form(occurrence.Id.ToString(), "Fictícia", Executor(f), ImagemFixtures.Png, original);
        using var response = await client.PostAsync("/api/imagens", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(expected, (await response.Content.ReadFromJsonAsync<ImagemMetadadosResponse>())!.NomeArquivo);
    }

    [Theory]
    [InlineData("/api/imagens/999", HttpStatusCode.NotFound)]
    [InlineData("/api/imagens/999/conteudo", HttpStatusCode.NotFound)]
    [InlineData("/api/imagens/0", HttpStatusCode.BadRequest)]
    [InlineData("/api/imagens/0/conteudo", HttpStatusCode.BadRequest)]
    public async Task IdInexistenteOuInvalidoRetornaProblemDetails(string path, HttpStatusCode expected)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task LeituraMetadadosEConteudoNaoRastreiaEntidades()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        var occurrence = await OccurrenceApiTests.CreateAsync(client);
        using var form = Form(occurrence.Id.ToString(), "Fictícia", Executor(f), ImagemFixtures.Png);
        var metadata = (await (await client.PostAsync("/api/imagens", form)).Content.ReadFromJsonAsync<ImagemMetadadosResponse>())!;
        using var scope = f.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ImagensService>();
        await service.ObterAsync(metadata.Id, default);
        var content = await service.AbrirConteudoAsync(metadata.Id, default);
        await using (content.Conteudo) Assert.Equal(ImagemFixtures.Png.Length, content.Conteudo.Length);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>().ChangeTracker.Entries());
    }

    [Fact]
    public async Task FalhaInternaNaoExpoeDetalhesNemPersisteImagem()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        var occurrence = await OccurrenceApiTests.CreateAsync(client);
        await using var failing = f.WithWebHostBuilder(b => b.ConfigureServices(s =>
            s.AddDbContext<ZelaSpDbContext>(o => o.AddInterceptors(new ImageFailure()))));
        using var failingClient = failing.CreateClient();
        using var form = Form(occurrence.Id.ToString(), "Fictícia", Executor(f), ImagemFixtures.Png);
        using var response = await failingClient.PostAsync("/api/imagens", form);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("INTERNAL_IMAGE_SENTINEL", body);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        using var scope = f.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>().ImagensOcorrencia.ToListAsync());
    }

    private sealed class ImageFailure : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,
            InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (data.Context!.ChangeTracker.Entries<ImagemOcorrencia>().Any(x => x.State == EntityState.Added))
                throw new InvalidOperationException("INTERNAL_IMAGE_SENTINEL");
            return base.SavingChangesAsync(data, result, ct);
        }
    }
}
