using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Xunit;
using ZelaSP.Application;
using ZelaSP.Domain;
using ZelaSP.Infrastructure;

namespace ZelaSP.Tests;

public sealed class RiscoAiTests
{
    internal const string TextoSeguro = "Evite incluir informações pessoais nos relatos. Este indicador não mede perigo no bairro.";
    internal static readonly ResumoRiscoAi Resumo = new(25, NivelRisco.MEDIO, new(2, 1, 0, 1, 0, 0));
    internal static HttpResponseMessage Envelope(string texto) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new { status = "completed", output = new[] { new { type = "message", role = "assistant", status = "completed",
            content = new[] { new { type = "output_text", text = JsonSerializer.Serialize(new { texto }) } } } } })
    };
    private static RiscoAiOptions Ativa() => new() { Enabled = true, ApiKey = "VALOR_FICTICIO_DE_TESTE_NAO_E_CHAVE" };
    internal sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        public int Chamadas { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        { Chamadas++; return action(request, ct); }
    }
    private static OpenAiExplicacaoCidadaClient Client(HttpClient http, RiscoAiOptions? config = null) => new(http, Options.Create(config ?? Ativa()));

    [Fact]
    public async Task ResponsesApiTemContratoEstritoESomenteAgregados()
    {
        using var handler = new Handler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://api.openai.com/v1/responses", request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            var root = json.RootElement;
            Assert.Equal("gpt-4.1-mini", root.GetProperty("model").GetString());
            Assert.False(root.GetProperty("store").GetBoolean());
            Assert.False(root.GetProperty("stream").GetBoolean());
            Assert.Equal("json_schema", root.GetProperty("text").GetProperty("format").GetProperty("type").GetString());
            using var payload = JsonDocument.Parse(root.GetProperty("input").GetString()!);
            Assert.Equal(new[] { "deteccoes", "nivel", "periodoDias", "pontuacao", "vazamentosBloqueados" },
                payload.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray());
            Assert.Equal(25, payload.RootElement.GetProperty("pontuacao").GetInt32());
            Assert.Equal(7, payload.RootElement.GetProperty("periodoDias").GetInt32());
            Assert.Equal(2, payload.RootElement.GetProperty("deteccoes").GetProperty("cpf").GetInt64());
            return Envelope(TextoSeguro);
        });
        using var http = new HttpClient(handler);
        Assert.Equal(TextoSeguro, await Client(http).GerarAsync(Resumo, default));
        Assert.Equal(1, handler.Chamadas);
    }

    [Theory]
    [InlineData("desabilitada")]
    [InlineData("sem-chave")]
    [InlineData("provider")]
    [InlineData("modelo")]
    [InlineData("resumo")]
    [InlineData("chave-invalida")]
    public async Task ConfiguracaoAusenteOuInvalidaNaoFazRequest(string caso)
    {
        var config = Ativa();
        var resumo = Resumo;
        switch (caso)
        {
            case "desabilitada": config.Enabled = false; break;
            case "sem-chave": config.ApiKey = null; break;
            case "provider": config.Provider = "NaoSuportado"; break;
            case "modelo": config.Model = "invalido\r\n"; break;
            case "resumo": resumo = Resumo with { Pontuacao = 100 }; break;
            case "chave-invalida": config.ApiKey = "FICTICIA\0"; break;
        }
        using var handler = new Handler((_, _) => throw new InvalidOperationException("Não deve enviar"));
        using var http = new HttpClient(handler);
        Assert.Null(await Client(http, config).GerarAsync(resumo, default));
        Assert.Equal(0, handler.Chamadas);
    }

    [Theory]
    [InlineData("cpf", "O dado 12345678909 apareceu.")]
    [InlineData("telefone", "Ligue para (11) 91234-0000.")]
    [InlineData("email", "Envie para ficticio@example.test.")]
    [InlineData("senha", "senha: segredo ficticio")]
    [InlineData("saude", "Há diabetes no relato.")]
    [InlineData("html", "<script>texto</script>")]
    [InlineData("url", "Consulte https://example.test")]
    [InlineData("vazia", "")]
    public async Task ConteudoNaoConfiavelRejeitado(string _, string texto)
    {
        using var handler = new Handler((_, _) => Task.FromResult(Envelope(texto)));
        using var http = new HttpClient(handler);
        Assert.Null(await Client(http).GerarAsync(Resumo, default));
    }

    [Theory]
    [InlineData("http")]
    [InlineData("rede")]
    [InlineData("json")]
    [InlineData("estrutura")]
    [InlineData("longo")]
    [InlineData("corpo")]
    [InlineData("refusal")]
    [InlineData("incomplete")]
    [InlineData("campos-extras")]
    public async Task FalhasGeramAusenciaDeTextoSemRetry(string caso)
    {
        using var handler = new Handler((_, _) => Task.FromResult(caso switch
        {
            "rede" => throw new HttpRequestException("Falha fictícia"),
            "http" => new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            "json" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{") },
            "estrutura" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") },
            "longo" => Envelope(new string('a', 601)),
            "corpo" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string('a', 32769)) },
            "refusal" => Raw("""{"status":"completed","output":[{"type":"message","role":"assistant","status":"completed","content":[{"type":"refusal","refusal":"Não"}]}]}"""),
            "incomplete" => Raw("""{"status":"incomplete","output":[]}"""),
            _ => Raw("""{"status":"completed","output":[{"type":"message","role":"assistant","status":"completed","content":[{"type":"output_text","text":"{\"texto\":\"Seguro\",\"pontuacao\":100}"}]}]}""")
        }));
        using var http = new HttpClient(handler);
        Assert.Null(await Client(http).GerarAsync(Resumo, default));
        Assert.Equal(1, handler.Chamadas);
    }

    private static HttpResponseMessage Raw(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task TimeoutRealDoAdapterRetornaFallbackSemRetry()
    {
        using var handler = new Handler(async (_, ct) => { await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, ct); return Envelope(TextoSeguro); });
        using var http = new HttpClient(handler) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
        Assert.Equal(TimeSpan.FromSeconds(5), OpenAiExplicacaoCidadaClient.Timeout);
        Assert.Null(await Client(http).GerarAsync(Resumo, default).WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(1, handler.Chamadas);
    }

    [Fact]
    public async Task CancelamentoDoClientePropaga()
    {
        using var started = new CancellationTokenSource();
        using var cancel = new CancellationTokenSource();
        using var handler = new Handler(async (_, ct) => { started.Cancel(); await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, ct); return Envelope(TextoSeguro); });
        using var http = new HttpClient(handler);
        var task = Client(http).GerarAsync(Resumo, cancel.Token);
        Assert.True(started.IsCancellationRequested);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TimeSpan.FromSeconds(2)));
        using var preCancel = new CancellationTokenSource();
        preCancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Client(http).GerarAsync(Resumo, preCancel.Token));
        Assert.Equal(1, handler.Chamadas);
    }

    [Fact]
    public async Task ModeloEhConfiguravelSemAlterarAgregados()
    {
        var config = Ativa();
        config.Model = "modelo-ficticio";
        using var handler = new Handler(async (request, ct) =>
        {
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Assert.Equal(config.Model, body.RootElement.GetProperty("model").GetString());
            return Envelope(TextoSeguro);
        });
        using var http = new HttpClient(handler);
        Assert.Equal(TextoSeguro, await Client(http, config).GerarAsync(Resumo, default));
    }

    [Fact]
    public async Task RespostaSemContentLengthTambemTemLimiteEfetivo()
    {
        using var stream = new StreamSemTamanho();
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) }));
        using var http = new HttpClient(handler);
        Assert.Null(await Client(http).GerarAsync(Resumo, default));
        Assert.Equal(OpenAiExplicacaoCidadaClient.MaximoRespostaBytes + 1, stream.Lidos);
    }

    private sealed class StreamSemTamanho : Stream
    {
        public int Lidos { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) { Array.Fill(buffer, (byte)'a', offset, count); Lidos += count; return count; }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        { ct.ThrowIfCancellationRequested(); buffer.Span.Fill((byte)'a'); Lidos += buffer.Length; return ValueTask.FromResult(buffer.Length); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
