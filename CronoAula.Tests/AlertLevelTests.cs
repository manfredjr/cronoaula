using CronoAula.ViewModels;

namespace CronoAula.Tests;

/// <summary>
/// Regras de cor: amarelo nos ultimos 20% do tempo ou nos ultimos 2 minutos
/// (o que for menor), laranja no ultimo minuto, vermelho ao zerar ou estourar.
/// </summary>
public class AlertLevelTests
{
    [Fact]
    public void TempoConfortavel_Normal()
    {
        var nivel = MainViewModel.ComputeAlert(TimeSpan.FromMinutes(8), TimeSpan.FromMinutes(10));
        Assert.Equal(AlertLevel.Normal, nivel);
    }

    [Fact]
    public void AulaLonga_UsaLimiteDeDoisMinutos()
    {
        // Em 50 min, 20% seriam 10 min; o limite menor (2 min) prevalece.
        var duracao = TimeSpan.FromMinutes(50);

        Assert.Equal(AlertLevel.Normal, MainViewModel.ComputeAlert(TimeSpan.FromMinutes(5), duracao));
        Assert.Equal(AlertLevel.Atencao, MainViewModel.ComputeAlert(TimeSpan.FromMinutes(1.8), duracao));
    }

    [Fact]
    public void AtividadeCurta_UsaOsVintePorCento()
    {
        // Em 5 min, 20% = 1 min, que e menor que 2 min.
        var duracao = TimeSpan.FromMinutes(5);

        Assert.Equal(AlertLevel.Normal, MainViewModel.ComputeAlert(TimeSpan.FromMinutes(3), duracao));
        // Abaixo de 1 min ja entra no laranja (regra do ultimo minuto tem precedencia).
        Assert.Equal(AlertLevel.Urgente, MainViewModel.ComputeAlert(TimeSpan.FromSeconds(50), duracao));
    }

    [Fact]
    public void UltimoMinuto_Urgente()
    {
        var nivel = MainViewModel.ComputeAlert(TimeSpan.FromSeconds(45), TimeSpan.FromMinutes(10));
        Assert.Equal(AlertLevel.Urgente, nivel);
    }

    [Fact]
    public void Zero_Estourado()
    {
        var nivel = MainViewModel.ComputeAlert(TimeSpan.Zero, TimeSpan.FromMinutes(10));
        Assert.Equal(AlertLevel.Estourado, nivel);
    }

    [Fact]
    public void TempoNegativo_Estourado()
    {
        var nivel = MainViewModel.ComputeAlert(TimeSpan.FromSeconds(-80), TimeSpan.FromMinutes(10));
        Assert.Equal(AlertLevel.Estourado, nivel);
    }

    [Fact]
    public void ProgressaoCompleta_NaoPula_Faixas()
    {
        var duracao = TimeSpan.FromMinutes(10);

        Assert.Equal(AlertLevel.Normal, MainViewModel.ComputeAlert(TimeSpan.FromMinutes(9), duracao));
        Assert.Equal(AlertLevel.Atencao, MainViewModel.ComputeAlert(TimeSpan.FromMinutes(1.9), duracao));
        Assert.Equal(AlertLevel.Urgente, MainViewModel.ComputeAlert(TimeSpan.FromSeconds(30), duracao));
        Assert.Equal(AlertLevel.Estourado, MainViewModel.ComputeAlert(TimeSpan.FromSeconds(-1), duracao));
    }
}
