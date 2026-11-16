using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ZelaSP.Application;

namespace ZelaSP.Api.Controllers;

[ApiController, Route("api/imagens")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public sealed class ImagensController(ImagensService service) : ControllerBase
{
    [HttpPost, SemBindingFormulario, RequestSizeLimit(LeitorUploadImagem.MaximoCorpo)]
    [ProducesResponseType(typeof(ImagemMetadadosResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<ImagemMetadadosResponse>> Enviar(CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var command = await LeitorUploadImagem.LerAsync(Request, buffer, ct);
        var metadata = await service.EnviarAsync(command, ct);
        return CreatedAtAction(nameof(Obter), new { id = metadata.Id }, metadata);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ImagemMetadadosResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImagemMetadadosResponse>> Obter([Range(1, int.MaxValue)] int id, CancellationToken ct) =>
        Ok(await service.ObterAsync(id, ct));

    [HttpGet("{id:int}/conteudo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Conteudo([Range(1, int.MaxValue)] int id, CancellationToken ct)
    {
        var content = await service.AbrirConteudoAsync(id, ct);
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(content.Conteudo, content.Mime);
    }
}
