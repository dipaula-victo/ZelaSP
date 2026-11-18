using Microsoft.AspNetCore.Mvc;
using ZelaSP.Application;

namespace ZelaSP.Api.Controllers;

[ApiController, Route("api/risco-cidadao")]
public sealed class RiscoCidadaoController(RiscoCidadaoService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(RiscoCidadaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Consultar indicador global Risco Cidadão")]
    [EndpointDescription("Últimos sete dias UTC, limites inclusivos, eventos futuros excluídos. Pesos CPF 6, TELEFONE 3, EMAIL 3, SENHA 10, SAUDE 8 e VAZAMENTO_BLOQUEADO 15 sem duplicar categoria. Máximo 100; BAIXO 0–19, MEDIO 20–49, ALTO 50–100. Consulta sem gravações. Alertas fixos Mobile e explicação LLM ou fallback separados. IA indisponível não impede HTTP 200; falha de banco retorna 500 genérico.")]
    public async Task<ActionResult<RiscoCidadaoResponse>> Obter(CancellationToken ct) => Ok(await service.ObterAsync(ct));
}
