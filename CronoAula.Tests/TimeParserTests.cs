using CronoAula.Core;

namespace CronoAula.Tests;

public class TimeParserTests
{
    [Theory]
    [InlineData("25:30", 0, 25, 30)]
    [InlineData("05:00", 0, 5, 0)]
    [InlineData("0:45", 0, 0, 45)]
    [InlineData("90:00", 1, 30, 0)]   // 90 minutos = 1h30
    public void TryParse_MmSs(string input, int h, int m, int s)
    {
        Assert.True(TimeParser.TryParse(input, out var t));
        Assert.Equal(new TimeSpan(h, m, s), t);
    }

    [Theory]
    [InlineData("01:05:00", 1, 5, 0)]
    [InlineData("02:00:30", 2, 0, 30)]
    public void TryParse_HhMmSs(string input, int h, int m, int s)
    {
        Assert.True(TimeParser.TryParse(input, out var t));
        Assert.Equal(new TimeSpan(h, m, s), t);
    }

    [Theory]
    [InlineData("25", 25)]
    [InlineData("5", 5)]
    [InlineData("50", 50)]
    public void TryParse_ApenasMinutos(string input, int minutes)
    {
        Assert.True(TimeParser.TryParse(input, out var t));
        Assert.Equal(TimeSpan.FromMinutes(minutes), t);
    }

    [Theory]
    [InlineData("25.5")]
    [InlineData("25,5")]
    public void TryParse_MinutosComFracao(string input)
    {
        Assert.True(TimeParser.TryParse(input, out var t));
        Assert.Equal(TimeSpan.FromMinutes(25.5), t);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("10:99")]      // segundos invalidos
    [InlineData("1:2:3:4")]    // componentes demais
    [InlineData("-5")]         // negativo
    [InlineData(null)]
    public void TryParse_EntradaInvalida(string? input)
    {
        Assert.False(TimeParser.TryParse(input, out _));
    }

    [Theory]
    [InlineData(0, 5, 0, "05:00")]
    [InlineData(0, 25, 30, "25:30")]
    [InlineData(1, 5, 0, "01:05:00")]
    public void Format_Positivo(int h, int m, int s, string esperado)
    {
        Assert.Equal(esperado, TimeParser.Format(new TimeSpan(h, m, s)));
    }

    [Fact]
    public void Format_Negativo_UsaPrefixo()
    {
        Assert.Equal("-01:20", TimeParser.Format(new TimeSpan(0, -1, -20)));
    }

    [Fact]
    public void Format_ExibeHorasApartirDeUmaHora()
    {
        Assert.Equal("01:00:00", TimeParser.Format(TimeSpan.FromHours(1)));
        Assert.Equal("59:00", TimeParser.Format(TimeSpan.FromMinutes(59)));
    }
}
