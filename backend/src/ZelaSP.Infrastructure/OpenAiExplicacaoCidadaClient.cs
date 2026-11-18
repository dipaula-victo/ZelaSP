using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using ZelaSP.Application;
using ZelaSP.Domain;

namespace ZelaSP.Infrastructure;

public sealed class RiscoAiOptions
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "OpenAI";
    public string Model { get; set; } = "gpt-4.1-mini";
    public string? ApiKey { get; set; }
}

public sealed class OpenAiExplicacaoCidadaClient(HttpClient http, IOptions<RiscoAiOptions> options) : IExplicacaoCidadaClient
{
    private static string? SemTexto(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return null; }
    public static readonly Uri Endpoint = new("https://api.openai.com/v1/responses");
    public const int MaximoRespostaBytes = 32 * 1024;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static readonly Regex ModeloValido = new(@"\A[A-Za-z0-9][A-Za-z0-9._:-]{0,99}\z", RegexOptions.NonBacktracking);

    public async Task<string?> GerarAsync(ResumoRiscoAi resumo, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var config = options.Value;
        if (!config.Enabled || config.Provider != "OpenAI" || string.IsNullOrWhiteSpace(config.ApiKey) ||
            config.ApiKey.Length > 1024 || config.ApiKey.Any(c => c <= 32 || c >= 127) || !ModeloValido.IsMatch(config.Model ?? "") ||
            !resumo.Contagens.Validas || CalculadoraRiscoCidadao.Calcular(resumo.Contagens) != (resumo.Pontuacao, resumo.Nivel)) return SemTexto(ct);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);
        try
        {
            // The input is reconstructed from a closed aggregate contract, never from entities/free text.
            var agregado = new
            {
                periodoDias = 7, pontuacao = resumo.Pontuacao, nivel = resumo.Nivel.ToString(),
                deteccoes = new { cpf = resumo.Contagens.Cpf, telefone = resumo.Contagens.Telefone, email = resumo.Contagens.Email,
                    senha = resumo.Contagens.Senha, saude = resumo.Contagens.Saude },
                vazamentosBloqueados = resumo.Contagens.VazamentosBloqueados
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
            request.Content = JsonContent.Create(new
            {
                model = config.Model, store = false, stream = false, max_output_tokens = 300,
                instructions = "Explique em português claro o indicador de exposição de dados do sistema. Não é perigo no bairro. " +
                    "Use apenas os agregados informados. Não invente fatos, dados pessoais ou exemplos. Não redefina pontuação ou nível. " +
                    "Retorne somente JSON com a propriedade texto, uma explicação curta em texto simples, sem links ou informações médicas específicas.",
                input = JsonSerializer.Serialize(agregado),
                text = new { format = new
                {
                    type = "json_schema", name = "explicacao_cidada", strict = true,
                    schema = new { type = "object", properties = new { texto = new { type = "string" } },
                        required = new[] { "texto" }, additionalProperties = false }
                } }
            });
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaximoRespostaBytes) return SemTexto(ct);
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[4096];
            while (true)
            {
                var read = await stream.ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, MaximoRespostaBytes - (int)buffer.Length + 1)), timeout.Token);
                if (read == 0) break;
                if (buffer.Length + read > MaximoRespostaBytes) return SemTexto(ct);
                await buffer.WriteAsync(chunk.AsMemory(0, read), timeout.Token);
            }
            using var json = JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            var root = json.RootElement;
            if (root.GetProperty("status").GetString() != "completed" ||
                (root.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)) return SemTexto(ct);
            string? textoJson = null;
            foreach (var output in root.GetProperty("output").EnumerateArray())
            {
                if (output.GetProperty("type").GetString() != "message" || output.GetProperty("role").GetString() != "assistant" ||
                    output.GetProperty("status").GetString() != "completed") return SemTexto(ct);
                foreach (var content in output.GetProperty("content").EnumerateArray())
                {
                    if (content.GetProperty("type").GetString() != "output_text" || textoJson is not null) return SemTexto(ct);
                    textoJson = content.GetProperty("text").GetString();
                }
            }
            if (textoJson is null) return SemTexto(ct);
            using var explicacao = JsonDocument.Parse(textoJson, new JsonDocumentOptions { MaxDepth = 2 });
            if (explicacao.RootElement.ValueKind != JsonValueKind.Object || explicacao.RootElement.EnumerateObject().Count() != 1) return SemTexto(ct);
            var texto = explicacao.RootElement.GetProperty("texto").GetString();
            ct.ThrowIfCancellationRequested();
            return ValidadorExplicacaoCidada.Aceita(texto) ? texto : null;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return SemTexto(ct); }
        catch (HttpRequestException) { ct.ThrowIfCancellationRequested(); return SemTexto(ct); }
        catch (IOException) { ct.ThrowIfCancellationRequested(); return SemTexto(ct); }
        catch (JsonException) { return SemTexto(ct); }
        catch (InvalidOperationException) { return SemTexto(ct); }
        catch (KeyNotFoundException) { return SemTexto(ct); }
        catch (FormatException) { return SemTexto(ct); }
    }
}
