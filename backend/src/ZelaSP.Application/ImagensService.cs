using System.Text;
using ZelaSP.Domain;

namespace ZelaSP.Application;

public sealed class ImagensService(IImagensStore store, TimeProvider clock, EquipeAcademica equipe)
{
    public async Task<ImagemMetadadosResponse> EnviarAsync(UploadImagemCommand request, CancellationToken ct)
    {
        if (request.OcorrenciaId <= 0) throw Invalid("ocorrenciaId", "Informe um identificador positivo.");
        var titulo = request.Titulo?.Trim();
        if (string.IsNullOrEmpty(titulo) || titulo.Length > 100)
            throw Invalid("titulo", "O título deve conter de 1 a 100 caracteres após trim.");
        if (!equipe.Permite(request.Executor)) throw Invalid("executor", "Informe exatamente um identificador acadêmico autorizado.");
        if (request.Arquivo is null) throw Invalid("arquivo", "Envie uma imagem PNG ou JPEG.");
        var nome = NormalizarNome(request.NomeArquivo);
        var extensao = Path.GetExtension(nome).ToLowerInvariant();
        var mime = request.Mime?.Trim().ToLowerInvariant();
        if (extensao is not (".png" or ".jpg" or ".jpeg") || mime is not ("image/png" or "image/jpeg"))
            throw Invalid("arquivo", "Aceitos somente PNG (.png) e JPEG (.jpg ou .jpeg), com MIME correspondente.");
        if (!await store.OcorrenciaExisteAsync(request.OcorrenciaId, ct))
            throw new RecursoNaoEncontradoException("Ocorrência não encontrada.");

        using var content = new MemoryStream();
        await LimitesImagem.CopiarLimitadoAsync(request.Arquivo, content, LimitesImagem.MaximoBytes, ct);
        var bytes = content.ToArray();
        if (bytes.Length == 0) throw Invalid("arquivo", "O arquivo não pode estar vazio.");
        var png = bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 });
        var mimeReal = png ? "image/png" : jpeg ? "image/jpeg" : null;
        if (mimeReal is null || mimeReal != mime || (extensao == ".png") != png)
            throw Invalid("arquivo", "Extensão, MIME e assinatura binária devem corresponder a PNG ou JPEG.");

        // Signature matching is format identification, not full decoding, integrity or malware validation.
        return await store.CriarAsync(new ImagemOcorrencia
        {
            OcorrenciaId = request.OcorrenciaId, Titulo = titulo, NomeArquivo = nome,
            Mime = mimeReal, Dados = bytes, DataUtc = clock.GetUtcNow().UtcDateTime,
            RmLider = equipe.RmLider, Executor = request.Executor!
        }, ct);
    }

    public async Task<ImagemMetadadosResponse> ObterAsync(int id, CancellationToken ct) =>
        await store.ObterMetadadosAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Imagem não encontrada.");

    public async Task<ConteudoImagemResponse> AbrirConteudoAsync(int id, CancellationToken ct)
    {
        var arquivo = await store.ObterConteudoAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Imagem não encontrada.");
        return new(new MemoryStream(arquivo.Dados, writable: false), arquivo.Mime);
    }

    private static string NormalizarNome(string? original)
    {
        if (string.IsNullOrWhiteSpace(original) || original.Any(char.IsControl))
            throw Invalid("arquivo", "Nome de arquivo inválido.");
        // Strip both platform path separators; the remaining value is metadata only.
        var leaf = original.Replace('\\', '/').Split('/')[^1].Trim();
        try { leaf = leaf.Normalize(NormalizationForm.FormC); }
        catch (ArgumentException) { throw Invalid("arquivo", "Nome de arquivo inválido."); }
        if (leaf.Length is 0 or > 255 || leaf.StartsWith('.') ||
            leaf.Any(c => "<>:\"/\\|?*".Contains(c) || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Format) ||
            string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(leaf)))
            throw Invalid("arquivo", "Nome de arquivo inválido ou maior que 255 caracteres.");
        return leaf;
    }

    private static ValidacaoAplicacaoException Invalid(string field, string message) => new(new() { [field] = [message] });
}
