using ZelaSP.Domain;

namespace ZelaSP.Application;

public sealed record DeteccoesRiscoResponse(long Cpf, long Telefone, long Email, long Senha, long Saude);
public sealed record AlertaRiscoResponse(string Tipo, string Texto);
public sealed record JanelaRiscoResponse(DateTime InicioUtc, DateTime FimUtc);
public sealed record ExplicacaoCidadaResponse(string Texto, string Origem);
public sealed record RiscoCidadaoResponse(int Pontuacao, NivelRisco Nivel, int PeriodoDias,
    DeteccoesRiscoResponse Deteccoes, long VazamentosBloqueados, IReadOnlyList<AlertaRiscoResponse> Alertas,
    JanelaRiscoResponse Janela, ExplicacaoCidadaResponse Explicacao);

// Only numbers and a closed enum. No entities, descriptions, IDs, names or free-form audit fields.
public sealed record ResumoRiscoAi(int Pontuacao, NivelRisco Nivel, ContagensRisco Contagens);

public interface IRiscoCidadaoStore
{
    Task<ContagensRisco> ContarAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken ct);
}

public interface IExplicacaoCidadaClient
{
    Task<string?> GerarAsync(ResumoRiscoAi resumo, CancellationToken ct);
}
