using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using ZelaSP.Application;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

public sealed class IoTApiTests
{
    private static readonly DateTime Agora = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);
    private static async Task<int[]> Dispositivos(OccurrenceApiFactory f, params string[] nomes)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.True(db.Database.IsSqlite());
        var devices = nomes.Select(nome => new Dispositivo { BairroId = 1, Nome = nome, Local = "Ponto exclusivamente fictício" }).ToArray();
        db.Dispositivos.AddRange(devices);
        await db.SaveChangesAsync();
        return devices.Select(d => d.Id).ToArray();
    }
    private static Task<HttpResponseMessage> Post(HttpClient client, string json) =>
        client.PostAsync("/api/leituras", new StringContent(json, Encoding.UTF8, "application/json"));

    [Fact]
    public async Task GetVazioRetornaArrayVazio()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        using var response = await client.GetAsync("/api/dispositivos");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.Empty((await response.Content.ReadFromJsonAsync<DispositivoResponse[]>())!);
    }

    [Fact]
    public async Task GetOrdenaDispositivosEUltimaLeituraComDesempateEmUmaConsulta()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        var ids = await Dispositivos(f, "Z fictício", "A fictício", "A fictício");
        int ultimaId;
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
            var empate = new LeituraSensor { DispositivoId = ids[1], NivelPercentual = 90.01m, LidaEm = Agora };
            db.LeiturasSensor.Add(new() { DispositivoId = ids[1], NivelPercentual = 89.99m, LidaEm = Agora });
            await db.SaveChangesAsync();
            db.LeiturasSensor.Add(empate);
            await db.SaveChangesAsync();
            ultimaId = empate.Id;
            // Higher ID with older timestamp must not become the latest reading.
            db.LeiturasSensor.Add(new() { DispositivoId = ids[1], NivelPercentual = 100m, LidaEm = Agora.AddDays(-1) });
            db.LeiturasSensor.Add(new() { DispositivoId = ids[0], NivelPercentual = 0m, LidaEm = Agora });
            await db.SaveChangesAsync();
        }
        var observer = new QueryObserver();
        await using var host = f.WithWebHostBuilder(b => b.ConfigureServices(s => s.AddDbContext<ZelaSpDbContext>(o => o.AddInterceptors(observer))));
        using var client = host.CreateClient();
        var result = (await client.GetFromJsonAsync<DispositivoResponse[]>("/api/dispositivos"))!;
        Assert.Equal(new[] { ids[1], ids[2], ids[0] }, result.Select(d => d.Id));
        Assert.Equal(new UltimaLeituraResponse(ultimaId, 90.01m, Agora, true), result[0].UltimaLeitura);
        Assert.Equal(DateTimeKind.Utc, result[0].UltimaLeitura!.LidaEm.Kind);
        Assert.Null(result[1].UltimaLeitura);
        Assert.False(result[2].UltimaLeitura!.NivelCritico);
        Assert.All(result, d => { Assert.Equal(1, d.BairroId); Assert.Equal("Ponto exclusivamente fictício", d.Local); });
        Assert.Single(observer.Comandos);
        using var fresh = host.Services.CreateScope();
        var readDb = fresh.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        await new IoTStore(readDb).ListarDispositivosAsync(default);
        Assert.Empty(readDb.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("0.00", false)]
    [InlineData("89.99", false)]
    [InlineData("90.00", true)]
    [InlineData("90.01", true)]
    [InlineData("100.00", true)]
    [InlineData("95.25", true)]
    [InlineData("90.0000", true)]
    [InlineData("1e2", true)]
    public async Task PostValidoPersisteNumeroExatoUtcEVinculo(string nivel, bool critico)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        var id = Assert.Single(await Dispositivos(f, "Sensor fictício"));
        using var client = f.CreateClient();
        using var response = await Post(client, $"{{\"dispositivoId\":{id},\"nivelPercentual\":{nivel}}}");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
        var result = (await response.Content.ReadFromJsonAsync<LeituraSensorResponse>())!;
        Assert.True(result.Id > 0);
        Assert.Equal(id, result.DispositivoId);
        Assert.Equal(decimal.Parse(nivel, NumberStyles.Float, CultureInfo.InvariantCulture), result.NivelPercentual);
        Assert.Equal(critico, result.NivelCritico);
        Assert.Equal(Agora, result.LidaEm);
        Assert.Equal(DateTimeKind.Utc, result.LidaEm.Kind);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        var saved = Assert.Single(await db.LeiturasSensor.AsNoTracking().ToListAsync());
        Assert.Equal(result.Id, saved.Id);
        Assert.Equal(id, saved.DispositivoId);
        Assert.Equal(result.NivelPercentual, saved.NivelPercentual);
        Assert.Equal(Agora, saved.LidaEm);
        var listed = Assert.Single((await client.GetFromJsonAsync<DispositivoResponse[]>("/api/dispositivos"))!);
        Assert.Equal(new UltimaLeituraResponse(result.Id, result.NivelPercentual, Agora, critico), listed.UltimaLeitura);
        Assert.Equal(1, await db.Dispositivos.CountAsync());
        Assert.Empty(await db.Ocorrencias.ToListAsync());
        Assert.Empty(await db.HistoricosStatus.ToListAsync());
        Assert.Empty(await db.EventosSeguranca.ToListAsync());
        Assert.Empty(await db.MensagensRisco.ToListAsync());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{")]
    [InlineData("{\"dispositivoId\":1}")]
    [InlineData("{\"nivelPercentual\":90}")]
    [InlineData("{\"dispositivoId\":null,\"nivelPercentual\":90}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":null}")]
    [InlineData("{\"dispositivoId\":0,\"nivelPercentual\":90}")]
    [InlineData("{\"dispositivoId\":-1,\"nivelPercentual\":90}")]
    [InlineData("{\"dispositivoId\":1.5,\"nivelPercentual\":90}")]
    [InlineData("{\"dispositivoId\":2147483648,\"nivelPercentual\":90}")]
    [InlineData("{\"dispositivoId\":\"1\",\"nivelPercentual\":90}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":\"90\"}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":\"FICTICIO_NAO_ECOAR\"}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":true}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":NaN}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":Infinity}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":-0.01}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":100.01}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":89.999}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":90.001}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":1e100}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":89.999999999999999999999999999999999999}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":90.000000000000000000000000000000000001}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":0.90001e2}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":90,\"id\":7}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":90,\"lidaEm\":\"2026-11-18T00:00:00Z\"}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":90,\"nivelCritico\":false}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":90,\"bairroId\":1}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":90,\"FICTICIO_NAO_ECOAR\":true}")]
    [InlineData("{\"dispositivoId\":1,\"dispositivoId\":2,\"nivelPercentual\":90}")]
    [InlineData("{\"dispositivoId\":1,\"nivelPercentual\":10,\"NivelPercentual\":90}")]
    public async Task PayloadInvalidoRetorna400SemGravarOuEcoar(string json)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        await Dispositivos(f, "Sensor fictício");
        using var client = f.CreateClient();
        using var response = await Post(client, json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("FICTICIO_NAO_ECOAR", body);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Empty(await db.LeiturasSensor.ToListAsync());
        Assert.Equal(1, await db.Dispositivos.CountAsync());
    }

    [Fact]
    public async Task DispositivoInexistenteRetorna404SemCriarDispositivo()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        using var response = await Post(client, "{\"dispositivoId\":999,\"nivelPercentual\":90}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Empty(await db.LeiturasSensor.ToListAsync());
        Assert.Empty(await db.Dispositivos.ToListAsync());
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("application/octet-stream")]
    public async Task ContentTypeIncorretoRetorna415(string contentType)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        using var response = await client.PostAsync("/api/leituras", new StringContent("{\"dispositivoId\":1,\"nivelPercentual\":90}", Encoding.UTF8, contentType));
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData(1024, false, HttpStatusCode.Created)]
    [InlineData(1024, true, HttpStatusCode.Created)]
    [InlineData(1025, false, HttpStatusCode.RequestEntityTooLarge)]
    [InlineData(1025, true, HttpStatusCode.RequestEntityTooLarge)]
    [InlineData(8192, true, HttpStatusCode.RequestEntityTooLarge)]
    public async Task LimiteEfetivoComOuSemContentLength(int tamanho, bool semLength, HttpStatusCode esperado)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        var id = Assert.Single(await Dispositivos(f, "Sensor fictício"));
        var json = $"{{\"dispositivoId\":{id},\"nivelPercentual\":90}}".PadRight(tamanho);
        using HttpContent content = semLength ? new SemLength(Encoding.UTF8.GetBytes(json)) : new StringContent(json, Encoding.UTF8, "application/json");
        if (semLength) Assert.Null(content.Headers.ContentLength);
        using var client = f.CreateClient();
        using var response = await client.PostAsync("/api/leituras", content);
        Assert.Equal(esperado, response.StatusCode);
        if (esperado == HttpStatusCode.RequestEntityTooLarge) Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var scope = f.Services.CreateScope();
        Assert.Equal(esperado == HttpStatusCode.Created ? 1 : 0, await scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>().LeiturasSensor.CountAsync());
    }

    [Fact]
    public async Task TimestampRecebimentoTruncaApenasSubMilissegundos()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        var id = Assert.Single(await Dispositivos(f, "Sensor fictício"));
        await using var host = f.WithWebHostBuilder(b => b.ConfigureServices(s =>
        { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(new ClockSubMilissegundos()); }));
        using var client = host.CreateClient();
        using var response = await Post(client, $"{{\"dispositivoId\":{id},\"nivelPercentual\":90}}");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var leitura = (await response.Content.ReadFromJsonAsync<LeituraSensorResponse>())!;
        Assert.Equal(Agora.AddMilliseconds(123), leitura.LidaEm);
        Assert.Equal(0, leitura.LidaEm.Ticks % TimeSpan.TicksPerMillisecond);
        Assert.Equal(DateTimeKind.Utc, leitura.LidaEm.Kind);
    }

    [Fact]
    public async Task ContratoEsp32EOpenApiSemLocationInventado()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        var texto = await client.GetStringAsync("/openapi/v1.json");
        using var json = JsonDocument.Parse(texto);
        var paths = json.RootElement.GetProperty("paths");
        var post = paths.GetProperty("/api/leituras").GetProperty("post");
        Assert.Contains("1024", post.GetProperty("description").GetString());
        var schema = post.GetProperty("requestBody").GetProperty("content").GetProperty("application/json");
        Assert.True(schema.TryGetProperty("schema", out _));
        var inputSchema = schema.GetProperty("schema");
        if (inputSchema.TryGetProperty("$ref", out var reference))
            inputSchema = json.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(reference.GetString()!.Split('/')[^1]);
        Assert.False(inputSchema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(new[] { "dispositivoId", "nivelPercentual" }, inputSchema.GetProperty("required").EnumerateArray().Select(x => x.GetString()).Order());
        Assert.Equal(0.01m, inputSchema.GetProperty("properties").GetProperty("nivelPercentual").GetProperty("multipleOf").GetDecimal());
        var example = schema.GetProperty("example");
        Assert.Equal(new[] { "dispositivoId", "nivelPercentual" }, example.EnumerateObject().Select(p => p.Name));
        Assert.False(post.GetProperty("responses").GetProperty("201").TryGetProperty("headers", out var headers) && headers.TryGetProperty("Location", out _));
        foreach (var code in new[] { "201", "400", "404", "413", "415", "500" }) Assert.True(post.GetProperty("responses").TryGetProperty(code, out _));
        Assert.True(paths.GetProperty("/api/dispositivos").GetProperty("get").GetProperty("responses").TryGetProperty("200", out _));
    }

    [Fact]
    public async Task LeiturasCriticasNaoAlteramDadosPreexistentesNemOutrasEntidades()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        var deviceId = Assert.Single(await Dispositivos(f, "Dispositivo preservado"));
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
            var occurrence = new Ocorrencia { BairroId = 1, CategoriaId = 1, Descricao = "Relato fictício preservado",
                Status = StatusOcorrencia.EM_ANALISE, Origem = OrigemOcorrencia.APP, CriadaEm = Agora.AddDays(-1) };
            var security = new EventoSeguranca { TipoEvento = "PII_DETECTADO", TipoDado = "CPF", Origem = "API_OCORRENCIAS",
                Destino = "BANCO", Acao = "MASCARADO", OcorridoEm = Agora.AddDays(-1) };
            db.AddRange(occurrence, security);
            await db.SaveChangesAsync();
            db.HistoricosStatus.Add(new() { OcorrenciaId = occurrence.Id, StatusAnterior = StatusOcorrencia.ABERTA,
                StatusNovo = StatusOcorrencia.EM_ANALISE, Executor = "executor-tecnico-ficticio", AlteradoEm = Agora.AddDays(-1) });
            db.ImagensOcorrencia.Add(new() { OcorrenciaId = occurrence.Id, Titulo = "Imagem fictícia preservada", NomeArquivo = "ficticia.png",
                Mime = "image/png", Dados = ImagemFixtures.Png, DataUtc = Agora.AddDays(-1), RmLider = "554787", Executor = "VictorHugoDePaula_RM554787" });
            db.MensagensRisco.Add(new() { EventoSegurancaId = security.Id, Mensagem = "Mensagem técnica fictícia preservada",
                OrigemMensagem = "FALLBACK", StatusTraducao = "CONCLUIDA", GeradaEm = Agora.AddDays(-1) });
            db.LeiturasSensor.Add(new() { DispositivoId = deviceId, NivelPercentual = 12.25m, LidaEm = Agora.AddDays(-1) });
            await db.SaveChangesAsync();
        }
        var antes = await Snapshot(f);
        using var client = f.CreateClient();
        using var first = await Post(client, $"{{\"dispositivoId\":{deviceId},\"nivelPercentual\":100}}");
        using var second = await Post(client, $"{{\"dispositivoId\":{deviceId},\"nivelPercentual\":100}}");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.NotEqual((await first.Content.ReadFromJsonAsync<LeituraSensorResponse>())!.Id,
            (await second.Content.ReadFromJsonAsync<LeituraSensorResponse>())!.Id);
        Assert.Equal(antes, await Snapshot(f));
        using var verify = f.Services.CreateScope();
        var dbVerify = verify.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Equal(3, await dbVerify.LeiturasSensor.CountAsync());
        var antiga = await dbVerify.LeiturasSensor.AsNoTracking().OrderBy(l => l.Id).FirstAsync();
        Assert.Equal(12.25m, antiga.NivelPercentual);
        Assert.Equal(Agora.AddDays(-1), antiga.LidaEm);
    }

    private static async Task<string[]> Snapshot(OccurrenceApiFactory f)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        return [JsonSerializer.Serialize(await db.Bairros.AsNoTracking().OrderBy(x => x.Id).ToListAsync()),
            JsonSerializer.Serialize(await db.CategoriasOcorrencia.AsNoTracking().OrderBy(x => x.Id).ToListAsync()),
            JsonSerializer.Serialize(await db.Dispositivos.AsNoTracking().OrderBy(x => x.Id).ToListAsync()),
            JsonSerializer.Serialize(await db.Ocorrencias.AsNoTracking().OrderBy(x => x.Id).ToListAsync()),
            JsonSerializer.Serialize(await db.HistoricosStatus.AsNoTracking().OrderBy(x => x.Id).ToListAsync()),
            JsonSerializer.Serialize(await db.ImagensOcorrencia.AsNoTracking().OrderBy(x => x.Id).ToListAsync()),
            JsonSerializer.Serialize(await db.EventosSeguranca.AsNoTracking().OrderBy(x => x.Id).ToListAsync()),
            JsonSerializer.Serialize(await db.MensagensRisco.AsNoTracking().OrderBy(x => x.Id).ToListAsync())];
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("100.01")]
    public async Task CheckNumericoDoSqlitePermaneceAtivo(string nivel)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        var id = Assert.Single(await Dispositivos(f, "Sensor fictício"));
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.True(db.Database.IsSqlite());
        var valor = decimal.Parse(nivel, CultureInfo.InvariantCulture);
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO LEITURA_SENSOR(dispositivo_id,nivel_percentual,lida_em) VALUES ({id},{valor},{Agora})"));
        Assert.Empty(await db.LeiturasSensor.ToListAsync());
    }

    [Fact]
    public async Task FalhaDePersistenciaRetorna500SemDetalhesOuLeituraParcial()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        var id = Assert.Single(await Dispositivos(f, "Sensor fictício"));
        await using var host = f.WithWebHostBuilder(b => b.ConfigureServices(s => s.AddDbContext<ZelaSpDbContext>(o => o.AddInterceptors(new FalhaLeitura()))));
        using var client = host.CreateClient();
        using var response = await Post(client, $"{{\"dispositivoId\":{id},\"nivelPercentual\":90}}");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.DoesNotContain("FICTICIO_FALHA_INTERNA", await response.Content.ReadAsStringAsync());
        using var scope = f.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>().LeiturasSensor.ToListAsync());
    }

    private sealed class FalhaLeitura : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<LeituraSensor>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("FICTICIO_FALHA_INTERNA");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class ClockSubMilissegundos : TimeProvider
    { public override DateTimeOffset GetUtcNow() => new DateTimeOffset(Agora).AddTicks(1234567); }

    private sealed class SemLength : HttpContent
    {
        private readonly byte[] bytes;
        public SemLength(byte[] body) { bytes = body; Headers.ContentType = new MediaTypeHeaderValue("application/json"); }
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(bytes).AsTask();
        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken ct) => stream.Write(bytes);
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken ct) => stream.WriteAsync(bytes, ct).AsTask();
        protected override Stream CreateContentReadStream(CancellationToken ct) => new MemoryStream(bytes, writable: false);
    }

    private sealed class QueryObserver : DbCommandInterceptor
    {
        public List<string> Comandos { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Comandos.Add(command.CommandText); return base.ReaderExecutingAsync(command, eventData, result, cancellationToken); }
    }
}
