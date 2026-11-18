using System.Globalization;
using Xunit;
using ZelaSP.Domain;

namespace ZelaSP.Tests;

public sealed class IoTTests
{
    [Theory]
    [InlineData("0.00", false)]
    [InlineData("89.99", false)]
    [InlineData("90.00", true)]
    [InlineData("90.01", true)]
    [InlineData("100.00", true)]
    public void RegraOficialEhDeterministica(string nivel, bool esperado) =>
        Assert.Equal(esperado, RegraNivelCritico.EhCritico(decimal.Parse(nivel, CultureInfo.InvariantCulture)));
}
