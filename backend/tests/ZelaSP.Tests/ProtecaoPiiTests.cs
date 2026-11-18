using Xunit;
using ZelaSP.Application;

namespace ZelaSP.Tests;

public sealed class ProtecaoPiiTests
{
    [Theory]
    [InlineData("senha: ficticia", "senha: [SENHA OCULTADA]", 1)]
    [InlineData("senha: minha senha ficticia", "senha: [SENHA OCULTADA]", 1)]
    [InlineData("senha=minha senha ficticia", "senha=[SENHA OCULTADA]", 1)]
    [InlineData("SENHA É minha senha ficticia", "SENHA É [SENHA OCULTADA]", 1)]
    [InlineData("senha: minha senha ficticia; relato público", "senha: [SENHA OCULTADA]; relato público", 1)]
    [InlineData("senha: minha senha ficticia, relato público", "senha: [SENHA OCULTADA], relato público", 1)]
    [InlineData("senha: minha.senha! ficticia? fim", "senha: [SENHA OCULTADA]", 1)]
    [InlineData("senha: minha senha ficticia\r\nrelato público", "senha: [SENHA OCULTADA]\r\nrelato público", 1)]
    [InlineData("senha: minha senha ficticia\nrelato público", "senha: [SENHA OCULTADA]\nrelato público", 1)]
    [InlineData("senha: minha senha ficticia\u2028relato público", "senha: [SENHA OCULTADA]\u2028relato público", 1)]
    [InlineData("senha: \"minha senha, ficticia; segura\" relato", "senha: \"[SENHA OCULTADA]\" relato", 1)]
    [InlineData("senha: 'minha senha ficticia' relato", "senha: '[SENHA OCULTADA]' relato", 1)]
    [InlineData("senha: 'senha=outra ficticia' texto longo preservado", "senha: '[SENHA OCULTADA]' texto longo preservado", 1)]
    [InlineData("senha: minha 12345678909 ficticio@example.test ficticia", "senha: [SENHA OCULTADA]", 1)]
    [InlineData("senha: primeira ficticia; senha=segunda ficticia", "senha: [SENHA OCULTADA]; senha=[SENHA OCULTADA]", 2)]
    [InlineData("senha: primeira ficticia senha é segunda ficticia", "senha: [SENHA OCULTADA] senha é [SENHA OCULTADA]", 2)]
    [InlineData("senha: [SENHA OCULTADA] texto público", "senha: [SENHA OCULTADA]", 1)]
    [InlineData("senha: \"ficticia sem fechamento e espaços", "senha: [SENHA OCULTADA]", 1)]
    public void SenhasComEspacosRespeitamLimitesConservadores(string entrada, string esperado, int deteccoes)
    {
        var resultado = ProtecaoPii.Analisar(entrada);
        Assert.Equal(esperado, resultado.Texto);
        Assert.Equal(deteccoes, resultado.Deteccoes.Count);
        Assert.All(resultado.Deteccoes, tipo => Assert.Equal(CategoriaPii.SENHA, tipo));
        var novamente = ProtecaoPii.Analisar(resultado.Texto);
        Assert.Equal(esperado, novamente.Texto);
        Assert.Empty(novamente.Deteccoes);
    }

    // Algorithmic CPF examples and reserved example.test addresses; no real personal data.
    [Theory]
    [InlineData("CPF 123.456.789-09.", "CPF [CPF OCULTADO].", CategoriaPii.CPF)]
    [InlineData("CPF 12345678909.", "CPF [CPF OCULTADO].", CategoriaPii.CPF)]
    [InlineData("11912345684", "[CPF OCULTADO]", CategoriaPii.CPF)]
    [InlineData("(11) 91234-0000", "[TELEFONE OCULTADO]", CategoriaPii.TELEFONE)]
    [InlineData("+55 (11) 2345-0000", "[TELEFONE OCULTADO]", CategoriaPii.TELEFONE)]
    [InlineData("11 2345 0000", "[TELEFONE OCULTADO]", CategoriaPii.TELEFONE)]
    [InlineData("11912340000", "[TELEFONE OCULTADO]", CategoriaPii.TELEFONE)]
    [InlineData("Contato: ficticio+teste@example.test.", "Contato: [E-MAIL OCULTADO].", CategoriaPii.EMAIL)]
    [InlineData("FICTICIO@EXAMPLE.TEST", "[E-MAIL OCULTADO]", CategoriaPii.EMAIL)]
    [InlineData("senha: SegredoFicticio! restante", "senha: [SENHA OCULTADA]", CategoriaPii.SENHA)]
    [InlineData("SENHA=Ficticia123; restante", "SENHA=[SENHA OCULTADA]; restante", CategoriaPii.SENHA)]
    [InlineData("Senha é 'duas palavras ficticias' e relato", "Senha é '[SENHA OCULTADA]' e relato", CategoriaPii.SENHA)]
    [InlineData("senha: \"ficticio@example.test\" fim", "senha: \"[SENHA OCULTADA]\" fim", CategoriaPii.SENHA)]
    [InlineData("senha: 11912345684", "senha: [SENHA OCULTADA]", CategoriaPii.SENHA)]
    [InlineData("senha: [CPF OCULTADO]segredoFicticio", "senha: [SENHA OCULTADA]", CategoriaPii.SENHA)]
    [InlineData("diabetes", "[SAÚDE OCULTADA]", CategoriaPii.SAUDE)]
    [InlineData("HIPERTENSÃO", "[SAÚDE OCULTADA]", CategoriaPii.SAUDE)]
    [InlineData("hipertensao", "[SAÚDE OCULTADA]", CategoriaPii.SAUDE)]
    [InlineData("câncer", "[SAÚDE OCULTADA]", CategoriaPii.SAUDE)]
    [InlineData("CANCER", "[SAÚDE OCULTADA]", CategoriaPii.SAUDE)]
    [InlineData("HIV", "[SAÚDE OCULTADA]", CategoriaPii.SAUDE)]
    [InlineData("Depressão", "[SAÚDE OCULTADA]", CategoriaPii.SAUDE)]
    [InlineData("depressao", "[SAÚDE OCULTADA]", CategoriaPii.SAUDE)]
    [InlineData("epilepsia", "[SAÚDE OCULTADA]", CategoriaPii.SAUDE)]
    [InlineData("hipertensa\u0303o", "[SAÚDE OCULTADA]", CategoriaPii.SAUDE)]
    public void DetectaSemRetornarValorOriginal(string entrada, string esperado, CategoriaPii tipo)
    {
        var resultado = ProtecaoPii.Analisar(entrada);
        Assert.Equal(esperado, resultado.Texto);
        Assert.Equal(tipo, Assert.Single(resultado.Deteccoes));
        var novamente = ProtecaoPii.Analisar(resultado.Texto);
        Assert.Equal(resultado.Texto, novamente.Texto);
        Assert.Empty(novamente.Deteccoes);
    }

    [Theory]
    [InlineData("12345678900")]
    [InlineData("123.456.789-00")]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    [InlineData("123456789")]
    [InlineData("a12345678909b")]
    [InlineData("(00) 91234-0000")]
    [InlineData("(11) 1234-0000")]
    [InlineData("91234-0000")]
    [InlineData("Lixeira cheia na praça. Texto exclusivamente fictício.")]
    [InlineData("diabetesXYZ anticancer HIVpositivo")]
    [InlineData("")]
    public void PreservaTextoNaoReconhecido(string entrada)
    {
        var resultado = ProtecaoPii.Analisar(entrada);
        Assert.Equal(entrada, resultado.Texto);
        Assert.Empty(resultado.Deteccoes);
    }

    [Fact]
    public void MultiplosTiposERepeticoesGeramUmaClassificacaoPorTrecho()
    {
        var resultado = ProtecaoPii.Analisar("12345678909 123.456.789-09 (11) 91234-0000 ficticio@example.test senha=Ficticia; diabetes HIV");
        Assert.Equal(7, resultado.Deteccoes.Count);
        Assert.Equal(2, resultado.Deteccoes.Count(x => x == CategoriaPii.CPF));
        Assert.Equal(2, resultado.Deteccoes.Count(x => x == CategoriaPii.SAUDE));
        Assert.Equal(5, resultado.Deteccoes.Distinct().Count());
        Assert.Empty(ProtecaoPii.Analisar(resultado.Texto).Deteccoes);
    }

    [Fact]
    public void SobreposicaoEmailSaudeESenhaTemPrioridadeDeterministica()
    {
        var resultado = ProtecaoPii.Analisar("diabetes@example.test senha: \"HIV 12345678909\"");
        Assert.Equal("[E-MAIL OCULTADO] senha: \"[SENHA OCULTADA]\"", resultado.Texto);
        Assert.Equal(new[] { CategoriaPii.EMAIL, CategoriaPii.SENHA }, resultado.Deteccoes);
    }

    [Theory]
    [InlineData("[CPF OCULTADO] [TELEFONE OCULTADO] [E-MAIL OCULTADO] [SENHA OCULTADA] [SAÚDE OCULTADA]")]
    [InlineData("senha: [SENHA OCULTADA]; restante")]
    [InlineData("senha: [CPF OCULTADO]; restante")]
    public void RotulosConhecidosNaoSaoNovasDeteccoes(string texto)
    {
        var resultado = ProtecaoPii.Analisar(texto);
        Assert.Equal(texto, resultado.Texto);
        Assert.Empty(resultado.Deteccoes);
    }

    [Fact]
    public void SenhaComRotuloParcialNaoEscapaDoMascaramento()
    {
        var resultado = ProtecaoPii.Analisar("senha: \"segredo [CPF OCULTADO] ficticio\"");
        Assert.Equal("senha: \"[SENHA OCULTADA]\"", resultado.Texto);
        Assert.Equal(CategoriaPii.SENHA, Assert.Single(resultado.Deteccoes));
    }

    [Theory]
    [InlineData("[CPF OCULTADO]")]
    [InlineData("[TELEFONE OCULTADO]")]
    [InlineData("[E-MAIL OCULTADO]")]
    [InlineData("[SENHA OCULTADA]")]
    [InlineData("[SAÚDE OCULTADA]")]
    public void RotuloComSufixoDentroDaSenhaNaoProtegeOSufixo(string rotulo)
    {
        foreach (var entrada in new[] { $"senha: {rotulo} SegredoFicticio123; público", $"senha: \"{rotulo} SegredoFicticio123\"; público" })
        {
            var resultado = ProtecaoPii.Analisar(entrada);
            var esperado = entrada.Contains('"') ? "senha: \"[SENHA OCULTADA]\"; público" : "senha: [SENHA OCULTADA]; público";
            Assert.Equal(esperado, resultado.Texto);
            Assert.Equal(CategoriaPii.SENHA, Assert.Single(resultado.Deteccoes));
            for (var i = 0; i < 3; i++)
            {
                var novamente = ProtecaoPii.Analisar(resultado.Texto);
                Assert.Equal(resultado.Texto, novamente.Texto);
                Assert.Empty(novamente.Deteccoes);
            }
        }
    }

    [Theory]
    [InlineData("[CPF OCULTADO]")]
    [InlineData("[TELEFONE OCULTADO]")]
    [InlineData("[E-MAIL OCULTADO]")]
    [InlineData("[SENHA OCULTADA]")]
    [InlineData("[SAÚDE OCULTADA]")]
    public void ValorInteiroIgualARotuloMantemIdempotencia(string rotulo)
    {
        foreach (var entrada in new[] { $"senha: {rotulo}", $"senha: {rotulo}  ; público", $"senha: '{rotulo}'; público" })
        {
            var texto = entrada;
            for (var i = 0; i < 3; i++)
            {
                var resultado = ProtecaoPii.Analisar(texto);
                Assert.Equal(entrada, resultado.Texto);
                Assert.Empty(resultado.Deteccoes);
                texto = resultado.Texto;
            }
        }
    }
}
