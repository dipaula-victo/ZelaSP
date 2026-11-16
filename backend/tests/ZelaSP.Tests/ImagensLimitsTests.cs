using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ZelaSP.Application;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

public sealed class ImagensLimitsTests
{
    [Fact]
    public async Task LeituraRealLimitadaNaoDependeDeLengthOuSeek()
    {
        using var source = new EndlessStream();
        using var destination = new MemoryStream();
        await Assert.ThrowsAsync<ConteudoMuitoGrandeException>(() =>
            LimitesImagem.CopiarLimitadoAsync(source, destination, LimitesImagem.MaximoBytes, default));
        Assert.Equal(LimitesImagem.MaximoBytes + 1, source.BytesRead);
        Assert.Equal(LimitesImagem.MaximoBytes, destination.Length);
    }

    [Fact]
    public async Task LeituraRespeitaCancelamento()
    {
        using var source = new EndlessStream();
        using var destination = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            LimitesImagem.CopiarLimitadoAsync(source, destination, LimitesImagem.MaximoBytes, cancellation.Token));
        Assert.Equal(0, source.BytesRead);
        Assert.Equal(0, destination.Length);
    }

    [Theory]
    [InlineData("arquivo-grande-sem-length")]
    [InlineData("epilogo-grande-sem-length")]
    [InlineData("multipart-incompleto")]
    [InlineData("boundary-ausente")]
    [InlineData("utf8-invalido")]
    [InlineData("campo-texto-grande")]
    public async Task MultipartMalformadoOuAcimaDoLimiteNaoGrava(string scenario)
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        var occurrence = await OccurrenceApiTests.CreateAsync(client);
        var executor = f.Services.GetRequiredService<EquipeAcademica>().Executores[0];
        var boundary = "ficticio-boundary";
        byte[] body;
        var expected = HttpStatusCode.BadRequest;
        if (scenario == "arquivo-grande-sem-length")
        {
            var prefix = Encoding.UTF8.GetBytes($"--{boundary}\r\nContent-Disposition: form-data; name=\"arquivo\"; filename=\"ficticia.png\"\r\nContent-Type: image/png\r\n\r\n");
            body = [.. prefix, .. new byte[LimitesImagem.MaximoBytes + 1], .. Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n")];
            expected = HttpStatusCode.RequestEntityTooLarge;
        }
        else if (scenario == "epilogo-grande-sem-length")
        {
            var prefix = Encoding.UTF8.GetBytes($"--{boundary}\r\nContent-Disposition: form-data; name=\"ocorrenciaId\"\r\n\r\n{occurrence.Id}\r\n--{boundary}\r\nContent-Disposition: form-data; name=\"titulo\"\r\n\r\nFictícia\r\n--{boundary}\r\nContent-Disposition: form-data; name=\"executor\"\r\n\r\n{executor}\r\n--{boundary}\r\nContent-Disposition: form-data; name=\"arquivo\"; filename=\"ficticia.png\"\r\nContent-Type: image/png\r\n\r\n");
            body = [.. prefix, .. ImagemFixtures.Png, .. Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n"), .. new byte[LimitesImagem.MaximoBytes + 64 * 1024]];
            expected = HttpStatusCode.RequestEntityTooLarge;
        }
        else if (scenario == "utf8-invalido")
            body = [.. Encoding.UTF8.GetBytes($"--{boundary}\r\nContent-Disposition: form-data; name=\"titulo\"\r\n\r\n"), 255, 255, .. Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n")];
        else if (scenario == "campo-texto-grande")
            body = Encoding.UTF8.GetBytes($"--{boundary}\r\nContent-Disposition: form-data; name=\"titulo\"\r\n\r\n" + new string('x', 4097) + $"\r\n--{boundary}--\r\n");
        else body = Encoding.UTF8.GetBytes($"--{boundary}\r\nContent-Disposition: form-data; name=\"titulo\"\r\n\r\nsem-fechamento");
        using var raw = new NonSeekableStream(body);
        using var content = new StreamContent(raw);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(scenario == "boundary-ausente"
            ? "multipart/form-data" : "multipart/form-data; boundary=" + boundary);
        Assert.Null(content.Headers.ContentLength);
        using var response = await client.PostAsync("/api/imagens", content);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZelaSpDbContext>();
        Assert.Empty(await db.ImagensOcorrencia.AsNoTracking().ToListAsync());
        Assert.Equal(ZelaSP.Domain.StatusOcorrencia.ABERTA, (await db.Ocorrencias.AsNoTracking().SingleAsync()).Status);
        Assert.Empty(await db.HistoricosStatus.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task OpenApiPublicaMultipartBinarioEquipeEMetadadosSemDados()
    {
        await using var f = await OccurrenceApiFactory.CreateAsync();
        using var client = f.CreateClient();
        using var json = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var paths = json.RootElement.GetProperty("paths");
        var post = paths.GetProperty("/api/imagens").GetProperty("post");
        var media = post.GetProperty("requestBody").GetProperty("content").GetProperty("multipart/form-data");
        var schema = media.GetProperty("schema");
        Assert.Equal(4, schema.GetProperty("required").GetArrayLength());
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        var properties = schema.GetProperty("properties");
        Assert.Equal("binary", properties.GetProperty("arquivo").GetProperty("format").GetString());
        Assert.Equal(LimitesImagem.MaximoBytes, properties.GetProperty("arquivo").GetProperty("maxLength").GetInt32());
        var members = properties.GetProperty("executor").GetProperty("enum").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(f.Services.GetRequiredService<EquipeAcademica>().Executores, members);
        Assert.False(properties.TryGetProperty("rmLider", out _));
        foreach (var code in new[] { "201", "400", "404", "413", "500" }) Assert.True(post.GetProperty("responses").TryGetProperty(code, out _));
        Assert.True(post.GetProperty("responses").GetProperty("201").GetProperty("headers").TryGetProperty("Location", out _));
        var binary = paths.GetProperty("/api/imagens/{id}/conteudo").GetProperty("get").GetProperty("responses").GetProperty("200").GetProperty("content");
        Assert.True(binary.TryGetProperty("image/png", out _));
        Assert.True(binary.TryGetProperty("image/jpeg", out _));
        Assert.False(binary.TryGetProperty("application/json", out _));
        var metadata = json.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("ImagemMetadadosResponse").GetProperty("properties");
        Assert.Equal(9, metadata.EnumerateObject().Count());
        Assert.False(metadata.TryGetProperty("dados", out _));
    }

    private sealed class EndlessStream : Stream
    {
        public int BytesRead { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            buffer.Span.Fill(1);
            BytesRead += buffer.Length;
            return ValueTask.FromResult(buffer.Length);
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class NonSeekableStream(byte[] bytes) : Stream
    {
        private readonly MemoryStream _source = new(bytes, writable: false);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => _source.ReadAsync(buffer, ct);
        public override int Read(byte[] buffer, int offset, int count) => _source.Read(buffer, offset, count);
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) _source.Dispose(); base.Dispose(disposing); }
    }
}
