using Microsoft.AspNetCore.Mvc;
using ZelaSP.Application;

namespace ZelaSP.Api.Controllers;

[ApiController, Route("api/categorias-ocorrencia")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public sealed class CategoriasOcorrenciaController(OcorrenciasService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoriaOcorrenciaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoriaOcorrenciaResponse>>> Listar(CancellationToken ct) =>
        Ok(await service.ListarCategoriasAsync(ct));
}
