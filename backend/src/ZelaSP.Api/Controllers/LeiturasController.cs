using Microsoft.AspNetCore.Mvc;
using ZelaSP.Application;
using ZelaSP.Api;

namespace ZelaSP.Api.Controllers;

[ApiController, Route("api/leituras")]
public sealed class LeiturasController(IoTService service) : ControllerBase
{
    [HttpPost, Consumes("application/json"), LimiteCorpoLeitura, RequestSizeLimit(LimiteCorpoLeituraAttribute.MaximoBytes)]
    [ProducesResponseType(typeof(LeituraSensorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Registrar percentual de preenchimento de um dispositivo")]
    [EndpointDescription("JSON com dispositivoId inteiro positivo existente e nivelPercentual numérico de 0 a 100, até duas casas efetivas, sem arredondamento. Corpo máximo 1024 bytes, mesmo sem Content-Length. Campos extras/duplicados/controlados são rejeitados. Servidor define id, lidaEm UTC de recebimento em milissegundos e nivelCritico (>=90). 201 sem Location: não há GET individual. Sem autenticação ou idempotência; retries podem duplicar leituras.")]
    public async Task<ActionResult<LeituraSensorResponse>> Registrar([FromBody] RegistrarLeituraRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.RegistrarAsync(request, ct));
}
