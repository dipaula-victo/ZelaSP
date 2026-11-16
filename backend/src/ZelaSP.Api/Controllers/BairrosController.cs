using Microsoft.AspNetCore.Mvc;
using ZelaSP.Application;

namespace ZelaSP.Api.Controllers;

[ApiController, Route("api/bairros")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public sealed class BairrosController(OcorrenciasService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BairroResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BairroResponse>>> Listar(CancellationToken ct) =>
        Ok(await service.ListarBairrosAsync(ct));
}
