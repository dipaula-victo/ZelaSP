namespace ZelaSP.Application;

public sealed class RecursoNaoEncontradoException(string mensagem) : Exception(mensagem);
public sealed class ConflitoEstadoException(string mensagem) : Exception(mensagem);
public sealed class ValidacaoAplicacaoException(Dictionary<string, string[]> erros)
    : Exception("Um ou mais campos são inválidos.")
{
    public Dictionary<string, string[]> Erros { get; } = erros;
}

public sealed class IdentidadeAuditoria
{
    public string Executor { get; }

    public IdentidadeAuditoria(string executor)
    {
        if (string.IsNullOrWhiteSpace(executor) || executor.Length > 120)
            throw new ArgumentException("Configure Auditoria:Executor com 1 a 120 caracteres.", nameof(executor));
        Executor = executor;
    }
}
