namespace ZelaSP.Domain;

public enum NivelRisco { BAIXO, MEDIO, ALTO }

public sealed record ContagensRisco(long Cpf, long Telefone, long Email, long Senha, long Saude, long VazamentosBloqueados)
{
    public bool Validas => Cpf >= 0 && Telefone >= 0 && Email >= 0 && Senha >= 0 && Saude >= 0 && VazamentosBloqueados >= 0;
}

public static class CalculadoraRiscoCidadao
{
    public static (int Pontuacao, NivelRisco Nivel) Calcular(ContagensRisco contagens)
    {
        if (!contagens.Validas) throw new ArgumentOutOfRangeException(nameof(contagens));
        // Saturate before multiplication too: even very large counts cannot overflow.
        var pontos = Math.Min(100, Math.Min(100, contagens.Cpf) * 6 + Math.Min(100, contagens.Telefone) * 3 +
            Math.Min(100, contagens.Email) * 3 + Math.Min(100, contagens.Senha) * 10 +
            Math.Min(100, contagens.Saude) * 8 + Math.Min(100, contagens.VazamentosBloqueados) * 15);
        return ((int)pontos, pontos < 20 ? NivelRisco.BAIXO : pontos < 50 ? NivelRisco.MEDIO : NivelRisco.ALTO);
    }
}
