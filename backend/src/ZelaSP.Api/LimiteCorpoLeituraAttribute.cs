using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Filters;
using ZelaSP.Application;

namespace ZelaSP.Api;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LimiteCorpoLeituraAttribute : Attribute, IAsyncResourceFilter
{
    public const int MaximoBytes = 1024;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var ct = context.HttpContext.RequestAborted;
        if (request.ContentLength > MaximoBytes) throw new ConteudoMuitoGrandeException();
        using var limitado = new MemoryStream();
        var buffer = new byte[256];
        while (true)
        {
            var read = await request.Body.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, MaximoBytes - (int)limitado.Length + 1)), ct);
            if (read == 0) break;
            if (limitado.Length + read > MaximoBytes) throw new ConteudoMuitoGrandeException();
            await limitado.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        // Reject duplicate property names too; case-insensitive MVC binding must not
        // turn two values for the same field into an ambiguous last-value-wins request.
        if (request.HasJsonContentType())
        {
            try
            {
                using var json = JsonDocument.Parse(limitado.ToArray(), new JsonDocumentOptions { MaxDepth = 8 });
                if (json.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var nomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (json.RootElement.EnumerateObject().Any(p => !nomes.Add(p.Name))) throw Invalido();
                    foreach (var campo in json.RootElement.EnumerateObject())
                        if (campo.Name.Equals("nivelPercentual", StringComparison.OrdinalIgnoreCase) && campo.Value.ValueKind == JsonValueKind.Number &&
                            !PrecisaoEfetivaValida(campo.Value.GetRawText()))
                            throw new ValidacaoAplicacaoException(new() { ["nivelPercentual"] = ["Informe no máximo duas casas decimais efetivas, sem arredondamento."] });
                }
            }
            catch (JsonException) { throw Invalido(); }
        }
        limitado.Position = 0;
        var original = request.Body;
        request.Body = limitado;
        try { await next(); }
        finally { request.Body = original; }
    }

    private static ValidacaoAplicacaoException Invalido() => new(new() { ["request"] = ["JSON inválido ou com campos duplicados."] });

    private static bool PrecisaoEfetivaValida(string numero)
    {
        // Inspect the original token before decimal deserialization can round excess digits.
        // Scale = fractional digits - trailing zero digits - exponent, without expanding it.
        var partes = numero.Split('e', 'E');
        if (partes.Length > 1 && !int.TryParse(partes[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _)) return false;
        var expoente = partes.Length == 1 ? 0 : int.Parse(partes[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        var mantissa = partes[0];
        var ponto = mantissa.IndexOf('.');
        var casas = ponto < 0 ? 0 : mantissa.Length - ponto - 1;
        var digitos = mantissa.Where(char.IsAsciiDigit).ToArray();
        var zeros = 0;
        for (var i = digitos.Length - 1; i >= 0 && digitos[i] == '0'; i--) zeros++;
        if (zeros == digitos.Length) return true;
        return (long)casas - zeros - expoente <= 2;
    }
}
