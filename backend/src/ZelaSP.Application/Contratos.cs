using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ZelaSP.Domain;

namespace ZelaSP.Application;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CriarOcorrenciaRequest
{
    [Required, Range(1, int.MaxValue)]
    public int? BairroId { get; init; }

    [Required, Range(1, int.MaxValue)]
    public int? CategoriaId { get; init; }

    [Required, StringLength(4000, MinimumLength = 1)]
    public string? Descricao { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AtualizarStatusRequest
{
    [Required, EnumDataType(typeof(StatusOcorrencia))]
    public StatusOcorrencia? Status { get; init; }
}

public sealed class ListarOcorrenciasQuery
{
    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, 100)]
    public int TamanhoPagina { get; init; } = 20;

    [Range(1, int.MaxValue)]
    public int? BairroId { get; init; }

    [Range(1, int.MaxValue)]
    public int? CategoriaId { get; init; }

    [RegularExpression("^(ABERTA|EM_ANALISE|RESOLVIDA)$")]
    public string? Status { get; init; }
}

public sealed record BairroResponse(int Id, string Nome, string Subprefeitura);
public sealed record CategoriaOcorrenciaResponse(int Id, string Nome);
public sealed record OcorrenciaResponse(int Id, int BairroId, int CategoriaId, string Descricao,
    StatusOcorrencia Status, DateTime CriadaEm, OrigemOcorrencia Origem);
public sealed record HistoricoStatusResponse(int Id, StatusOcorrencia StatusAnterior,
    StatusOcorrencia StatusNovo, DateTime AlteradoEm, string Executor);
public sealed record PaginaOcorrenciasResponse(IReadOnlyList<OcorrenciaResponse> Itens,
    int Pagina, int TamanhoPagina, int TotalRegistros, int TotalPaginas);
