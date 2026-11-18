using Xunit;
using ZelaSP.Domain;

namespace ZelaSP.Tests;

public sealed class RiscoCidadaoTests
{
    [Theory]
    [InlineData(1, 0, 0, 0, 0, 0, 6)]
    [InlineData(0, 1, 0, 0, 0, 0, 3)]
    [InlineData(0, 0, 1, 0, 0, 0, 3)]
    [InlineData(0, 0, 0, 1, 0, 0, 10)]
    [InlineData(0, 0, 0, 0, 1, 0, 8)]
    [InlineData(0, 0, 0, 0, 0, 1, 15)]
    [InlineData(2, 1, 0, 1, 0, 0, 25)]
    [InlineData(0, 3, 0, 1, 0, 0, 19)]
    [InlineData(0, 0, 0, 2, 0, 0, 20)]
    [InlineData(0, 3, 0, 4, 0, 0, 49)]
    [InlineData(0, 0, 0, 5, 0, 0, 50)]
    [InlineData(0, 0, 0, 10, 0, 0, 100)]
    [InlineData(1, 1, 1, 20, 1, 1, 100)]
    [InlineData(0, 0, 0, 0, 0, 0, 0)]
    public void PesosSomaRepeticoesEClassificacao(long cpf, long telefone, long email, long senha, long saude, long bloqueios, int esperado)
    {
        var (pontos, nivel) = CalculadoraRiscoCidadao.Calcular(new(cpf, telefone, email, senha, saude, bloqueios));
        Assert.Equal(esperado, pontos);
        Assert.Equal(esperado < 20 ? NivelRisco.BAIXO : esperado < 50 ? NivelRisco.MEDIO : NivelRisco.ALTO, nivel);
    }

    [Fact]
    public void SaturacaoNaoEstouraNumerosGrandes() => Assert.Equal(100,
        CalculadoraRiscoCidadao.Calcular(new(long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue)).Pontuacao);

    [Fact]
    public void ContagensNegativasSaoRejeitadas() => Assert.Throws<ArgumentOutOfRangeException>(() =>
        CalculadoraRiscoCidadao.Calcular(new(-1, 0, 0, 0, 0, 0)));
}
