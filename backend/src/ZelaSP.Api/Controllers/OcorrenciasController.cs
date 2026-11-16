using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ZelaSP.Application;

namespace ZelaSP.Api.Controllers;

[ApiController, Route("api/ocorrencias")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public sealed class OcorrenciasController(OcorrenciasService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(OcorrenciaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OcorrenciaResponse>> Criar(CriarOcorrenciaRequest request, CancellationToken ct)
    {
        var response = await service.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = response.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginaOcorrenciasResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaOcorrenciasResponse>> Listar([FromQuery] ListarOcorrenciasQuery query, CancellationToken ct) =>
        Ok(await service.ListarAsync(query, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OcorrenciaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OcorrenciaResponse>> Obter([Range(1, int.MaxValue)] int id, CancellationToken ct) =>
        Ok(await service.ObterAsync(id, ct));

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(OcorrenciaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OcorrenciaResponse>> AtualizarStatus([Range(1, int.MaxValue)] int id,
        AtualizarStatusRequest request, CancellationToken ct) => Ok(await service.AtualizarStatusAsync(id, request, ct));

    [HttpGet("{id:int}/historico")]
    [ProducesResponseType(typeof(IReadOnlyList<HistoricoStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<HistoricoStatusResponse>>> Historico([Range(1, int.MaxValue)] int id, CancellationToken ct) =>
        Ok(await service.HistoricoAsync(id, ct));
}
