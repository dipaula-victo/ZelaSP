using Microsoft.AspNetCore.Mvc;
using ZelaSP.Application;

namespace ZelaSP.Api.Controllers;

[ApiController, Route("api/dispositivos")]
public sealed class DispositivosController(IoTService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DispositivoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Listar dispositivos e sua última leitura")]
    [EndpointDescription("Ordenação nome ASC, id ASC. Retorna [] quando vazio e ultimaLeitura null sem leituras. Última por lidaEm DESC, id DESC. Datas UTC; nivelCritico calculado como percentual >= 90.00, sem IA ou criação de ocorrência.")]
    public async Task<ActionResult<IReadOnlyList<DispositivoResponse>>> Listar(CancellationToken ct) =>
        Ok(await service.ListarDispositivosAsync(ct));
}
