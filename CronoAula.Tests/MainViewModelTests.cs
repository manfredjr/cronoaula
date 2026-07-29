using CronoAula.Core;
using CronoAula.ViewModels;

namespace CronoAula.Tests;

public class MainViewModelTests
{
    private static MainViewModel Criar(AppSettings? s = null) =>
        new(s ?? new AppSettings(), new SoundService());

    [Fact]
    public void AoAbrir_MostraOUltimoTempoUsado()
    {
        var vm = Criar();

        // Padrao de fabrica: 50 minutos.
        Assert.Equal("50:00", vm.Display);
        Assert.Equal(TimerState.Stopped, vm.Engine.State);
    }

    [Fact]
    public void AoAbrir_RestauraTempoSalvo()
    {
        var vm = Criar(new AppSettings { LastMinutes = 15 });

        Assert.Equal("15:00", vm.Display);
    }

    [Fact]
    public void PrimeiroClique_Carrega_SegundoClique_Inicia()
    {
        var vm = Criar();

        vm.PresetClicked(10);
        Assert.Equal("10:00", vm.Display);
        Assert.Equal(10, vm.ArmedPreset);
        Assert.Equal(TimerState.Stopped, vm.Engine.State);
        Assert.False(vm.IsRunning);

        vm.PresetClicked(10);
        Assert.True(vm.IsRunning);
        Assert.Null(vm.ArmedPreset);
    }

    [Fact]
    public void ClicarOutroPreset_Recarrega_SemIniciar()
    {
        var vm = Criar();

        vm.PresetClicked(10);
        vm.PresetClicked(30); // preset diferente: apenas carrega

        Assert.Equal("30:00", vm.Display);
        Assert.Equal(30, vm.ArmedPreset);
        Assert.False(vm.IsRunning);
    }

    [Fact]
    public void BotaoPrincipal_AlternaRotulo()
    {
        var vm = Criar();
        Assert.Equal("Iniciar", vm.PrimaryButtonLabel);

        vm.ToggleStartPause();
        Assert.Equal("Pausar", vm.PrimaryButtonLabel);

        vm.ToggleStartPause();
        Assert.Equal("Continuar", vm.PrimaryButtonLabel);
    }

    [Fact]
    public void MaisUmMinuto_EstendeOTempo()
    {
        var vm = Criar(new AppSettings { LastMinutes = 10 });

        vm.AddMinutes(1);

        Assert.Equal("11:00", vm.Display);
    }

    [Fact]
    public void MenosUmMinuto_NaoDeixaNegativaADuracao()
    {
        var vm = Criar(new AppSettings { LastMinutes = 0.5 });

        vm.AddMinutes(-1); // tentaria ir para -0,5 min

        Assert.Equal("00:00", vm.Display);
    }

    [Fact]
    public void Zerar_VoltaAoTempoCheio()
    {
        var vm = Criar(new AppSettings { LastMinutes = 20 });

        vm.PresetClicked(5);
        vm.Start();
        vm.Reset();

        Assert.Equal("05:00", vm.Display);
        Assert.False(vm.IsRunning);
    }

    [Fact]
    public void PreferenciaDeContagemNegativa_EAplicadaAoEngine()
    {
        var settings = new AppSettings { AllowOvertime = false };
        var vm = Criar(settings);

        Assert.False(vm.Engine.AllowOvertime);

        settings.AllowOvertime = true;
        vm.ApplySettings();

        Assert.True(vm.Engine.AllowOvertime);
    }

    [Fact]
    public void AvisoAntecipadoDesligado_NaoDefineLimiar()
    {
        var vm = Criar(new AppSettings { EarlyWarningEnabled = false });

        Assert.Null(vm.Engine.EarlyWarningAt);
    }

    [Fact]
    public void AvisoAntecipadoLigado_UsaOsMinutosConfigurados()
    {
        var vm = Criar(new AppSettings { EarlyWarningEnabled = true, EarlyWarningMinutes = 3 });

        Assert.Equal(TimeSpan.FromMinutes(3), vm.Engine.EarlyWarningAt);
    }
}
