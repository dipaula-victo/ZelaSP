using System.Text;
using System.Text.RegularExpressions;

namespace ZelaSP.Application;

public enum CategoriaPii { CPF, TELEFONE, EMAIL, SENHA, SAUDE }

// Results carry classifications only, never matched values or excerpts.
public sealed record ResultadoPii(string Texto, IReadOnlyList<CategoriaPii> Deteccoes);

public static class ProtecaoPii
{
    private sealed record Trecho(int Inicio, int Tamanho, CategoriaPii Tipo);
    private static Regex Pattern(string pattern) => new(pattern,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
        TimeSpan.FromMilliseconds(250));
    private static readonly Regex Rotulos = Pattern(@"\[(?:CPF OCULTADO|TELEFONE OCULTADO|E-MAIL OCULTADO|SENHA OCULTADA|SAÚDE OCULTADA)\]");
    private static readonly Regex Senha = Pattern(@"\bsenha[ \t]*(?::|=|é|e\u0301)[ \t]*");
    private static readonly Regex Cpf = Pattern(@"\b(?:[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2}|[0-9]{11})\b");
    private static readonly Regex Telefone = Pattern(@"(?:\+55[ .-]?)?(?:\([1-9][0-9]\)[ .-]?|[1-9][0-9][ .-]?)(?:9[0-9]{4}|[2-5][0-9]{3})[- .]?[0-9]{4}");
    private static readonly Regex Email = Pattern(@"[a-z0-9][a-z0-9._%+\-]{0,63}@[a-z0-9](?:[a-z0-9\-]{0,61}[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9\-]{0,61}[a-z0-9])?)+");
    private static readonly Regex Saude = Pattern(@"\b(?:diabetes|hipertens(?:ã|a\u0303|a)o|c(?:â|a\u0302|a)ncer|HIV|depress(?:ã|a\u0303|a)o|epilepsia)\b");
    private static readonly HashSet<string> Ddds = new("11 12 13 14 15 16 17 18 19 21 22 24 27 28 31 32 33 34 35 37 38 41 42 43 44 45 46 47 48 49 51 53 54 55 61 62 63 64 65 66 67 68 69 71 73 74 75 77 79 81 82 83 84 85 86 87 88 89 91 92 93 94 95 96 97 98 99".Split(' '));

    public static ResultadoPii Analisar(string texto)
    {
        var protegidos = Rotulos.Matches(texto).Cast<Match>().Select(m => (m.Index, m.Length)).ToArray();
        var candidatos = new List<Trecho>();
        void Adicionar(int inicio, int tamanho, CategoriaPii tipo)
        {
            if (!protegidos.Any(p => inicio < p.Index + p.Length && p.Index < inicio + tamanho))
                candidatos.Add(new(inicio, tamanho, tipo));
        }
        var senhas = Senha.Matches(texto).Cast<Match>().ToArray();
        var fimSenhaEntreAspas = 0;
        for (var indice = 0; indice < senhas.Length; indice++)
        {
            if (senhas[indice].Index < fimSenhaEntreAspas) continue;
            var inicio = senhas[indice].Index + senhas[indice].Length;
            if (inicio == texto.Length || DelimitadorSenha(texto[inicio])) continue;
            var fimLinha = inicio;
            while (fimLinha < texto.Length && !QuebraLinha(texto[fimLinha])) fimLinha++;
            var fechamento = texto[inicio] is '\'' or '"' ? texto.IndexOf(texto[inicio], inicio + 1) : -1;
            int fim;
            if (fechamento > inicio && fechamento < fimLinha)
            {
                fim = fechamento + 1;
                fimSenhaEntreAspas = fim;
            }
            else
            {
                // Unquoted/unterminated values conservatively include spaces and punctuation
                // until an explicit delimiter or another password declaration.
                fim = inicio;
                var proxima = indice + 1 < senhas.Length ? senhas[indice + 1].Index : texto.Length;
                while (fim < proxima && !DelimitadorSenha(texto[fim])) fim++;
                while (fim > inicio && char.IsWhiteSpace(texto[fim - 1])) fim--;
            }
            if (fim <= inicio) continue;
            // Only the entire delimited value may be considered already protected.
            // A label prefix, even followed by whitespace, does not protect its suffix.
            // Keep matching quotes in the output so subsequent passes retain the same boundary.
            var entreAspas = fechamento > inicio && fechamento < fimLinha;
            var inicioValor = entreAspas ? inicio + 1 : inicio;
            var fimValor = entreAspas ? fim - 1 : fim;
            var conteudo = texto[inicioValor..fimValor];
            if (!Rotulos.IsMatch(conteudo) || Rotulos.Match(conteudo).Length != conteudo.Length)
                candidatos.Add(new(inicioValor, fimValor - inicioValor, CategoriaPii.SENHA));
        }
        foreach (Match m in Email.Matches(texto))
            if (Limites(texto, m.Index, m.Length) && !m.Value.Split('@')[0].Contains(".."))
                Adicionar(m.Index, m.Length, CategoriaPii.EMAIL);
        foreach (Match m in Cpf.Matches(texto))
            if (Limites(texto, m.Index, m.Length) && CpfValido(m.Value)) Adicionar(m.Index, m.Length, CategoriaPii.CPF);
        foreach (Match m in Telefone.Matches(texto))
        {
            var digits = new string(m.Value.Where(char.IsAsciiDigit).ToArray());
            if (m.Value.StartsWith('+')) digits = digits[2..];
            if (Limites(texto, m.Index, m.Length) && Ddds.Contains(digits[..2]))
                Adicionar(m.Index, m.Length, CategoriaPii.TELEFONE);
        }
        foreach (Match m in Saude.Matches(texto)) Adicionar(m.Index, m.Length, CategoriaPii.SAUDE);

        // Higher priority wins the whole overlapping span; same priority: longest, then earliest.
        var selecionados = new List<Trecho>();
        foreach (var trecho in candidatos.OrderBy(x => Prioridade(x.Tipo)).ThenByDescending(x => x.Tamanho).ThenBy(x => x.Inicio))
            if (!selecionados.Any(x => x.Inicio < trecho.Inicio + trecho.Tamanho && trecho.Inicio < x.Inicio + x.Tamanho))
                selecionados.Add(trecho);
        var builder = new StringBuilder();
        var posicao = 0;
        foreach (var trecho in selecionados.OrderBy(x => x.Inicio))
        {
            builder.Append(texto, posicao, trecho.Inicio - posicao).Append(Rotulo(trecho.Tipo));
            posicao = trecho.Inicio + trecho.Tamanho;
        }
        builder.Append(texto, posicao, texto.Length - posicao);
        return new(builder.ToString(), Array.AsReadOnly(selecionados.OrderBy(x => x.Inicio).Select(x => x.Tipo).ToArray()));
    }

    private static bool QuebraLinha(char c) => c is '\r' or '\n' or '\u0085' or '\u2028' or '\u2029';
    private static bool DelimitadorSenha(char c) => c is ',' or ';' || QuebraLinha(c);

    private static bool Limites(string texto, int inicio, int tamanho) =>
        (inicio == 0 || !char.IsLetterOrDigit(texto[inicio - 1]) && texto[inicio - 1] != '_') &&
        (inicio + tamanho == texto.Length || !char.IsLetterOrDigit(texto[inicio + tamanho]) && texto[inicio + tamanho] != '_');

    private static bool CpfValido(string valor)
    {
        var digits = valor.Where(char.IsAsciiDigit).Select(c => c - '0').ToArray();
        if (digits.Length != 11 || digits.All(d => d == digits[0])) return false;
        for (var size = 9; size <= 10; size++)
        {
            var soma = 0;
            for (var i = 0; i < size; i++) soma += digits[i] * (size + 1 - i);
            var resto = soma % 11;
            if (digits[size] != (resto < 2 ? 0 : 11 - resto)) return false;
        }
        return true;
    }

    private static int Prioridade(CategoriaPii tipo) => tipo switch
    { CategoriaPii.SENHA => 0, CategoriaPii.EMAIL => 1, CategoriaPii.CPF => 2, CategoriaPii.TELEFONE => 3, _ => 4 };
    private static string Rotulo(CategoriaPii tipo) => tipo switch
    {
        CategoriaPii.CPF => "[CPF OCULTADO]", CategoriaPii.TELEFONE => "[TELEFONE OCULTADO]",
        CategoriaPii.EMAIL => "[E-MAIL OCULTADO]", CategoriaPii.SENHA => "[SENHA OCULTADA]", _ => "[SAÚDE OCULTADA]"
    };
}
