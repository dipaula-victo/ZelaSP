namespace ZelaSP.Application;

public sealed record UploadImagemCommand(int OcorrenciaId, string? Titulo, string? Executor,
    string? NomeArquivo, string? Mime, Stream? Arquivo);

public sealed record ImagemMetadadosResponse(int Id, int OcorrenciaId, string Titulo, string NomeArquivo,
    string Mime, int TamanhoBytes, DateTime DataUtc, string RmLider, string Executor);

public sealed record ImagemBinaria(byte[] Dados, string Mime);
public sealed record ConteudoImagemResponse(Stream Conteudo, string Mime);

public interface IImagensStore
{
    Task<bool> OcorrenciaExisteAsync(int id, CancellationToken ct);
    Task<ImagemMetadadosResponse> CriarAsync(ZelaSP.Domain.ImagemOcorrencia imagem, CancellationToken ct);
    Task<ImagemMetadadosResponse?> ObterMetadadosAsync(int id, CancellationToken ct);
    Task<ImagemBinaria?> ObterConteudoAsync(int id, CancellationToken ct);
}

public sealed class ConteudoMuitoGrandeException() : Exception("O arquivo excede o limite de 2 MiB.");
