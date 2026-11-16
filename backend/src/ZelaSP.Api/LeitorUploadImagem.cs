using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using ZelaSP.Application;

namespace ZelaSP.Api;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SemBindingFormularioAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        context.ValueProviderFactories.RemoveType<FormValueProviderFactory>();
        context.ValueProviderFactories.RemoveType<FormFileValueProviderFactory>();
        context.ValueProviderFactories.RemoveType<JQueryFormValueProviderFactory>();
    }
    public void OnResourceExecuted(ResourceExecutedContext context) { }
}

internal static class LeitorUploadImagem
{
    public const int MaximoCorpo = LimitesImagem.MaximoBytes + 64 * 1024;

    public static async Task<UploadImagemCommand> LerAsync(HttpRequest request, MemoryStream arquivo, CancellationToken ct)
    {
        if (request.ContentLength > MaximoCorpo) throw new ConteudoMuitoGrandeException();
        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType) ||
            !contentType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            throw Invalid("Envie multipart/form-data.");
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
        if (string.IsNullOrEmpty(boundary) || boundary.Length > 70) throw Invalid("Boundary multipart inválido.");

        using var limitedBody = new CorpoLimitado(request.Body, MaximoCorpo);
        var reader = new MultipartReader(boundary, limitedBody) { HeadersCountLimit = 16, HeadersLengthLimit = 8192 };
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? filename = null, mime = null;
        try
        {
            MultipartSection? section;
            while ((section = await reader.ReadNextSectionAsync(ct)) is not null)
            {
                if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition) ||
                    !disposition.DispositionType.Equals("form-data", StringComparison.OrdinalIgnoreCase))
                    throw Invalid("Parte multipart inválida.");
                var name = HeaderUtilities.RemoveQuotes(disposition.Name).Value;
                if (name is not ("ocorrenciaId" or "titulo" or "executor" or "arquivo") || !seen.Add(name))
                    throw Invalid("Envie somente os quatro campos previstos, uma única vez.");

                var hasFilename = disposition.FileName.HasValue || disposition.FileNameStar.HasValue;
                if (name == "arquivo")
                {
                    if (!hasFilename) throw Invalid("O campo arquivo deve conter uma imagem.");
                    filename = HeaderUtilities.RemoveQuotes(disposition.FileNameStar.HasValue ? disposition.FileNameStar : disposition.FileName).Value;
                    mime = section.ContentType;
                    await LimitesImagem.CopiarLimitadoAsync(section.Body, arquivo, LimitesImagem.MaximoBytes, ct);
                }
                else
                {
                    if (hasFilename) throw Invalid("Campos de texto não aceitam arquivos.");
                    using var value = new MemoryStream();
                    try { await LimitesImagem.CopiarLimitadoAsync(section.Body, value, 4096, ct); }
                    catch (ConteudoMuitoGrandeException) { throw Invalid("Campo de texto excede 4096 bytes."); }
                    fields[name] = new UTF8Encoding(false, true).GetString(value.ToArray());
                }
            }
            // Account for any epilogue too; do not accept an oversized chunked body
            // merely because the closing boundary appeared before its remaining bytes.
            await LimitesImagem.CopiarLimitadoAsync(limitedBody, Stream.Null, MaximoCorpo, ct);
        }
        catch (InvalidDataException) { throw Invalid("Multipart inválido ou incompleto."); }
        catch (IOException error) when (error is not BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge })
        { throw Invalid("Multipart inválido ou incompleto."); }
        catch (DecoderFallbackException) { throw Invalid("Campos de texto devem usar UTF-8 válido."); }
        if (!fields.TryGetValue("ocorrenciaId", out var id) ||
            !int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var occurrenceId) || occurrenceId <= 0)
            throw new ValidacaoAplicacaoException(new() { ["ocorrenciaId"] = ["Informe um identificador inteiro positivo."] });
        arquivo.Position = 0;
        return new(occurrenceId, fields.GetValueOrDefault("titulo"), fields.GetValueOrDefault("executor"),
            filename, mime, seen.Contains("arquivo") ? arquivo : null);
    }

    private static ValidacaoAplicacaoException Invalid(string message) => new(new() { ["multipart"] = [message] });

    // Bounds headers, preamble, text and framing too, including chunked requests.
    // Disposing this wrapper does not dispose ASP.NET's request body.
    private sealed class CorpoLimitado(Stream source, int limit) : Stream
    {
        private int _read;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _read; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = source.Read(buffer, offset, Math.Min(count, limit - _read + 1));
            _read += read;
            if (_read > limit) throw new ConteudoMuitoGrandeException();
            return read;
        }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            var read = await source.ReadAsync(buffer[..Math.Min(buffer.Length, limit - _read + 1)], ct);
            _read += read;
            if (_read > limit) throw new ConteudoMuitoGrandeException();
            return read;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
            ReadAsync(buffer.AsMemory(offset, count), ct).AsTask();
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
