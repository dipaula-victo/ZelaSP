using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ZelaSP.Application;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            ValidacaoAplicacaoException => StatusCodes.Status400BadRequest,
            RecursoNaoEncontradoException => StatusCodes.Status404NotFound,
            ConflitoEstadoException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        context.Response.StatusCode = status;
        ProblemDetails details = exception is ValidacaoAplicacaoException validation
            ? new HttpValidationProblemDetails(validation.Erros)
            : new ProblemDetails();
        details.Status = status;
        details.Title = status == 500 ? "Erro interno ao processar a solicitação." : exception.Message;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = details
        });
        return true;
    }
}
