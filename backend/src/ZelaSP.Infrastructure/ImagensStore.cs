using Microsoft.EntityFrameworkCore;
using ZelaSP.Application;
using ZelaSP.Domain;

namespace ZelaSP.Infrastructure;

public sealed class ImagensStore(ZelaSpDbContext db) : IImagensStore
{
    public Task<bool> OcorrenciaExisteAsync(int id, CancellationToken ct) => db.Ocorrencias.AsNoTracking().AnyAsync(x => x.Id == id, ct);

    public async Task<ImagemMetadadosResponse> CriarAsync(ImagemOcorrencia imagem, CancellationToken ct)
    {
        db.ImagensOcorrencia.Add(imagem);
        await db.SaveChangesAsync(ct);
        return new(imagem.Id, imagem.OcorrenciaId, imagem.Titulo, imagem.NomeArquivo, imagem.Mime,
            imagem.Dados.Length, DateTime.SpecifyKind(imagem.DataUtc, DateTimeKind.Utc), imagem.RmLider, imagem.Executor);
    }

    public Task<ImagemMetadadosResponse?> ObterMetadadosAsync(int id, CancellationToken ct) =>
        db.ImagensOcorrencia.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new ImagemMetadadosResponse(x.Id, x.OcorrenciaId, x.Titulo, x.NomeArquivo, x.Mime,
                x.Dados.Length, DateTime.SpecifyKind(x.DataUtc, DateTimeKind.Utc), x.RmLider, x.Executor)).SingleOrDefaultAsync(ct);

    public Task<ImagemBinaria?> ObterConteudoAsync(int id, CancellationToken ct) =>
        db.ImagensOcorrencia.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new ImagemBinaria(x.Dados, x.Mime)).SingleOrDefaultAsync(ct);
}
