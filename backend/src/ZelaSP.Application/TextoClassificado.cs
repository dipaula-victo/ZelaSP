using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ZelaSP.Application;

public enum ClassificacaoTexto { SemDeteccaoReconhecida, Sensivel, Indeterminada }

/// <summary>Explicit taint for supported operations, not a universal PII guarantee.</summary>
[DebuggerDisplay("[TEXTO CLASSIFICADO]")]
public sealed class TextoClassificado
{
    public const int MaximoCaracteres = 600;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly string? _conteudo;
    public ClassificacaoTexto Classificacao { get; }

    private TextoClassificado(string? conteudo, ClassificacaoTexto classificacao)
    {
        _conteudo = conteudo;
        Classificacao = classificacao;
    }

    public static TextoClassificado Classificar(string? conteudo)
    {
        if (conteudo is null || conteudo.Length > MaximoCaracteres)
            return new(conteudo, ClassificacaoTexto.Indeterminada);
        try
        {
            return new(conteudo, ProtecaoPii.Analisar(conteudo).Deteccoes.Count > 0
                ? ClassificacaoTexto.Sensivel : ClassificacaoTexto.SemDeteccaoReconhecida);
        }
        catch (RegexMatchTimeoutException)
        {
            // Never propagate the regex exception: it can retain the original input.
            return new(conteudo, ClassificacaoTexto.Indeterminada);
        }
    }

    public TextoClassificado Concatenar(TextoClassificado outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        // Bound the operation before allocating; neither taint nor uncertainty is cleared.
        if (_conteudo is null || outro._conteudo is null ||
            _conteudo.Length > MaximoCaracteres - outro._conteudo.Length)
            return new(null, Classificacao == ClassificacaoTexto.Sensivel || outro.Classificacao == ClassificacaoTexto.Sensivel
                ? ClassificacaoTexto.Sensivel : ClassificacaoTexto.Indeterminada);
        var combinado = Classificar(_conteudo + outro._conteudo);
        var classificacao = Classificacao == ClassificacaoTexto.Sensivel || outro.Classificacao == ClassificacaoTexto.Sensivel
            ? ClassificacaoTexto.Sensivel
            : Classificacao == ClassificacaoTexto.Indeterminada || outro.Classificacao == ClassificacaoTexto.Indeterminada
                ? ClassificacaoTexto.Indeterminada : combinado.Classificacao;
        return new(combinado._conteudo, classificacao);
    }

    public bool TentarPublicar([NotNullWhen(true)] out string? texto)
    {
        texto = null;
        if (Classificacao != ClassificacaoTexto.SemDeteccaoReconhecida ||
            string.IsNullOrWhiteSpace(_conteudo) || _conteudo.Length > MaximoCaracteres || _conteudo != _conteudo.Trim() ||
            _conteudo.Any(c => char.IsControl(c) || char.GetUnicodeCategory(c) == UnicodeCategory.Format || "<>[]{}\\/@".Contains(c)) ||
            _conteudo.Contains("http", StringComparison.OrdinalIgnoreCase)) return false;
        texto = _conteudo;
        return true;
    }

    public override string ToString() => "[TEXTO CLASSIFICADO]";
}
