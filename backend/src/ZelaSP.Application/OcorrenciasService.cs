using System.ComponentModel.DataAnnotations;
using ZelaSP.Domain;

namespace ZelaSP.Application;

public sealed class OcorrenciasService(IOcorrenciasStore store, TimeProvider clock, IdentidadeAuditoria auditoria)
{
    public Task<IReadOnlyList<BairroResponse>> ListarBairrosAsync(CancellationToken ct) => store.ListarBairrosAsync(ct);
    public Task<IReadOnlyList<CategoriaOcorrenciaResponse>> ListarCategoriasAsync(CancellationToken ct) => store.ListarCategoriasAsync(ct);

    public async Task<OcorrenciaResponse> CriarAsync(CriarOcorrenciaRequest request, CancellationToken ct)
    {
        Validar(request);
        var erros = new Dictionary<string, string[]>();
        if (!await store.BairroExisteAsync(request.BairroId!.Value, ct))
            erros["bairroId"] = ["Bairro não cadastrado."];
        if (!await store.CategoriaExisteAsync(request.CategoriaId!.Value, ct))
            erros["categoriaId"] = ["Categoria não cadastrada."];
        if (erros.Count > 0) throw new ValidacaoAplicacaoException(erros);

        return await store.CriarAsync(new Ocorrencia
        {
            BairroId = request.BairroId.Value, CategoriaId = request.CategoriaId.Value,
            Descricao = request.Descricao!.Trim(), Status = FluxoStatusOcorrencia.Inicial,
            Origem = OrigemOcorrencia.APP, CriadaEm = clock.GetUtcNow().UtcDateTime
        }, ct);
    }

    public async Task<OcorrenciaResponse> ObterAsync(int id, CancellationToken ct) =>
        await store.ObterAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Ocorrência não encontrada.");

    public Task<PaginaOcorrenciasResponse> ListarAsync(ListarOcorrenciasQuery query, CancellationToken ct)
    {
        Validar(query);
        if ((long)(query.Pagina - 1) * query.TamanhoPagina > int.MaxValue)
            throw new ValidacaoAplicacaoException(new() { ["pagina"] = ["Página excede o intervalo suportado."] });
        return store.ListarAsync(query, ct);
    }

    public async Task<OcorrenciaResponse> AtualizarStatusAsync(int id, AtualizarStatusRequest request, CancellationToken ct)
    {
        Validar(request);
        var atual = await ObterAsync(id, ct);
        var novo = request.Status!.Value;
        if (!FluxoStatusOcorrencia.Permite(atual.Status, novo))
            throw new ConflitoEstadoException("Transição de status não permitida.");

        await store.AlterarStatusComHistoricoAsync(id, atual.Status, novo,
            clock.GetUtcNow().UtcDateTime, auditoria.Executor, ct);
        // Return the state committed by this operation, even if another request advances it afterwards.
        return atual with { Status = novo };
    }

    public async Task<IReadOnlyList<HistoricoStatusResponse>> HistoricoAsync(int id, CancellationToken ct)
    {
        await ObterAsync(id, ct);
        return await store.HistoricoAsync(id, ct);
    }

    private static void Validar(object value)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(value, new ValidationContext(value), results, true)) return;
        var erros = results.SelectMany(r => r.MemberNames.DefaultIfEmpty("request")
            .Select(name => (name, message: r.ErrorMessage ?? "Valor inválido.")))
            .GroupBy(x => char.ToLowerInvariant(x.name[0]) + x.name[1..])
            .ToDictionary(g => g.Key, g => g.Select(x => x.message).ToArray());
        throw new ValidacaoAplicacaoException(erros);
    }
}
