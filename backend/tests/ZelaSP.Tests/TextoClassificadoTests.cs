using System.Reflection;
using System.Text.Json;
using Xunit;
using ZelaSP.Application;

namespace ZelaSP.Tests;

public sealed class TextoClassificadoTests
{
    [Theory]
    [InlineData("CPF 12345678909")]
    [InlineData("Telefone (11) 91234-0000")]
    [InlineData("ficticio@example.test")]
    [InlineData("senha: Segredo Ficticio")]
    [InlineData("Informacao sobre diabetes")]
    public void PiiReconhecidaMarcadaEBloqueada(string original)
    {
        var value = TextoClassificado.Classificar(original);
        Assert.Equal(ClassificacaoTexto.Sensivel, value.Classificacao);
        Assert.False(value.TentarPublicar(out var output));
        Assert.Null(output);
    }

    [Fact]
    public void TextoPermitidoPreservadoSemAfirmarAusenciaUniversalDePii()
    {
        const string text = "Evite incluir dados pessoais nos relatos.";
        var value = TextoClassificado.Classificar(text);
        Assert.Equal(ClassificacaoTexto.SemDeteccaoReconhecida, value.Classificacao);
        Assert.True(value.TentarPublicar(out var output));
        Assert.Equal(text, output);
        Assert.True(ValidadorExplicacaoCidada.Aceita(text));
    }

    [Fact]
    public void ConcatenacaoPropagaMarcacaoNasDuasOrdensSemMutarOriginais()
    {
        var sensitive = TextoClassificado.Classificar("senha: Segredo Ficticio");
        var common = TextoClassificado.Classificar("Aviso ");
        foreach (var result in new[] { common.Concatenar(sensitive), sensitive.Concatenar(common) })
        {
            Assert.Equal(ClassificacaoTexto.Sensivel, result.Classificacao);
            Assert.False(result.TentarPublicar(out var output));
            Assert.Null(output);
        }
        Assert.Equal(ClassificacaoTexto.Sensivel, sensitive.Classificacao);
        Assert.Equal(ClassificacaoTexto.SemDeteccaoReconhecida, common.Classificacao);
    }

    [Theory]
    [InlineData("ficticio", "@example.test")]
    [InlineData("senha", ": Segredo Ficticio")]
    public void ReclassificaPiiFormadaNaFronteiraDaConcatenacao(string left, string right)
    {
        var a = TextoClassificado.Classificar(left);
        var b = TextoClassificado.Classificar(right);
        Assert.Equal(ClassificacaoTexto.SemDeteccaoReconhecida, a.Classificacao);
        Assert.Equal(ClassificacaoTexto.SemDeteccaoReconhecida, b.Classificacao);
        var combined = a.Concatenar(b);
        Assert.Equal(ClassificacaoTexto.Sensivel, combined.Classificacao);
        Assert.False(combined.TentarPublicar(out var output));
        Assert.Null(output);
    }

    [Fact]
    public void ConcatenacaoPermitidaPreservaTexto()
    {
        var value = TextoClassificado.Classificar("Mensagem ").Concatenar(TextoClassificado.Classificar("segura."));
        Assert.True(value.TentarPublicar(out var output));
        Assert.Equal("Mensagem segura.", output);
    }

    [Fact]
    public void RepresentacaoESerializacaoNaoExpoemConteudoPrivado()
    {
        const string original = "senha: Segredo Ficticio";
        var value = TextoClassificado.Classificar(original);
        Assert.Equal("[TEXTO CLASSIFICADO]", value.ToString());
        Assert.Equal("[TEXTO CLASSIFICADO]", $"{value}");
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(value));
        Assert.Equal(new[] { "Classificacao" }, json.RootElement.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.DoesNotContain("Segredo", JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true }));
        Assert.Empty(typeof(TextoClassificado).GetConstructors());
        Assert.DoesNotContain(typeof(TextoClassificado).GetMethods(BindingFlags.Public | BindingFlags.Static), m => m.Name == "op_Implicit");
        Assert.All(typeof(TextoClassificado).GetProperties(), p => Assert.Null(p.SetMethod));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" Texto")]
    [InlineData("Texto ")]
    [InlineData("Texto\nextra")]
    [InlineData("Texto\u202Eoculto")]
    [InlineData("<script>texto</script>")]
    [InlineData("Texto [oculto]")]
    [InlineData("Texto {objeto}")]
    [InlineData("Caminho\\local")]
    [InlineData("Veja https://example.test")]
    [InlineData("Texto @local")]
    public void FormatoInvalidoNaoPublicaNemRetornaOriginal(string? original)
    {
        Assert.False(TextoClassificado.Classificar(original).TentarPublicar(out var output));
        Assert.Null(output);
        Assert.False(ValidadorExplicacaoCidada.Aceita(original));
    }

    [Fact]
    public void LimitesEIncertezaFalhamFechados()
    {
        Assert.True(TextoClassificado.Classificar(new string('x', 600)).TentarPublicar(out _));
        var excessive = TextoClassificado.Classificar(new string('x', 601));
        Assert.Equal(ClassificacaoTexto.Indeterminada, excessive.Classificacao);
        var common = TextoClassificado.Classificar("texto");
        foreach (var value in new[] { excessive, excessive.Concatenar(common), common.Concatenar(excessive),
            TextoClassificado.Classificar(null).Concatenar(common),
            TextoClassificado.Classificar(new string('x', 400)).Concatenar(TextoClassificado.Classificar(new string('x', 201))) })
        {
            Assert.False(value.TentarPublicar(out var output));
            Assert.Null(output);
        }
        var sensitive = TextoClassificado.Classificar("senha: Ficticia").Concatenar(excessive);
        Assert.Equal(ClassificacaoTexto.Sensivel, sensitive.Classificacao);
        Assert.False(sensitive.TentarPublicar(out _));
    }
}
