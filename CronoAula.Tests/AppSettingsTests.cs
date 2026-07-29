using System.IO;
using CronoAula.Core;

namespace CronoAula.Tests;

public class AppSettingsTests : IDisposable
{
    private readonly string _pasta;
    private readonly string _arquivo;

    public AppSettingsTests()
    {
        // Pasta temporaria propria: os testes nunca tocam no %APPDATA% real.
        _pasta = Path.Combine(Path.GetTempPath(), "CronoAulaTestes_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pasta);
        _arquivo = Path.Combine(_pasta, "config.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_pasta, recursive: true); } catch { /* limpeza best-effort */ }
    }

    [Fact]
    public void ArquivoAusente_CaiNosPadroes()
    {
        var s = AppSettings.LoadFrom(Path.Combine(_pasta, "nao-existe.json"));

        Assert.Equal(DisplaySize.Medio, s.Size);
        Assert.True(s.AllowOvertime);
        Assert.True(s.SoundEnabled);
        Assert.Equal(new List<int> { 5, 10, 15, 30, 50 }, s.Presets);
    }

    [Fact]
    public void ArquivoCorrompido_NaoDerruba_CaiNosPadroes()
    {
        File.WriteAllText(_arquivo, "{ isto nao e json valido ][");

        var s = AppSettings.LoadFrom(_arquivo);

        Assert.Equal(DisplaySize.Medio, s.Size);
        Assert.Equal(50, s.LastMinutes);
    }

    [Fact]
    public void ArquivoVazio_NaoDerruba()
    {
        File.WriteAllText(_arquivo, "");

        var s = AppSettings.LoadFrom(_arquivo);

        Assert.NotNull(s);
        Assert.Equal(DisplaySize.Medio, s.Size);
    }

    [Fact]
    public void SalvaERestaura_PosicaoTamanhoOpacidade()
    {
        var original = new AppSettings
        {
            Left = 320.5,
            Top = 118.25,
            Size = DisplaySize.Grande,
            Opacity = 0.65,
            LastMinutes = 30,
            SoundEnabled = false,
            Volume = 0.4,
            AllowOvertime = false,
            EarlyWarningMinutes = 3
        };

        original.SaveTo(_arquivo);
        var lido = AppSettings.LoadFrom(_arquivo);

        Assert.Equal(320.5, lido.Left);
        Assert.Equal(118.25, lido.Top);
        Assert.Equal(DisplaySize.Grande, lido.Size);
        Assert.Equal(0.65, lido.Opacity);
        Assert.Equal(30, lido.LastMinutes);
        Assert.False(lido.SoundEnabled);
        Assert.Equal(0.4, lido.Volume);
        Assert.False(lido.AllowOvertime);
        Assert.Equal(3, lido.EarlyWarningMinutes);
    }

    [Fact]
    public void ValoresForaDaFaixa_SaoCorrigidos()
    {
        // Simula um config.json editado a mao com valores absurdos.
        File.WriteAllText(_arquivo, """
        {
          "Opacity": 5.0,
          "Volume": -3.0,
          "EarlyWarningMinutes": 9999,
          "LastMinutes": -10,
          "Presets": []
        }
        """);

        var s = AppSettings.LoadFrom(_arquivo);

        Assert.InRange(s.Opacity, 0.30, 1.00);
        Assert.InRange(s.Volume, 0.0, 1.0);
        Assert.InRange(s.EarlyWarningMinutes, 1, 120);
        Assert.Equal(50, s.LastMinutes);
        Assert.NotEmpty(s.Presets);
    }

    [Fact]
    public void MonitorDaTelaCheia_ESalvoERestaurado()
    {
        var original = new AppSettings { FullscreenMonitor = @"\\.\DISPLAY2" };

        original.SaveTo(_arquivo);
        var lido = AppSettings.LoadFrom(_arquivo);

        Assert.Equal(@"\\.\DISPLAY2", lido.FullscreenMonitor);
    }

    [Fact]
    public void ConfigAntigo_SemOsCamposNovos_CarregaComPadroes()
    {
        // Um config.json gravado por uma versao anterior do programa nao pode
        // impedir a nova de abrir.
        File.WriteAllText(_arquivo, """
        { "Size": "Grande", "Opacity": 0.8, "LastMinutes": 25 }
        """);

        var s = AppSettings.LoadFrom(_arquivo);

        Assert.Equal(DisplaySize.Grande, s.Size);
        Assert.Equal(25, s.LastMinutes);
        // Campos que so existem na versao nova entram com o padrao.
        Assert.Equal(5, s.AlertRepetitions);
        Assert.Equal(3.0, s.AlertIntervalSeconds);
        Assert.Null(s.FullscreenMonitor);
        Assert.Equal("Ctrl+Alt+F", s.Hotkeys[nameof(HotkeyAction.TelaCheia)]);
    }

    [Fact]
    public void RepeticoesEIntervalo_ForaDaFaixa_SaoCorrigidos()
    {
        File.WriteAllText(_arquivo, """
        { "AlertRepetitions": 9999, "AlertIntervalSeconds": 0.01 }
        """);

        var s = AppSettings.LoadFrom(_arquivo);

        Assert.InRange(s.AlertRepetitions, 0, 60);
        Assert.InRange(s.AlertIntervalSeconds, 1.0, 30.0);
    }

    [Fact]
    public void RepeticoesZero_EPreservado_PoisSignificaAteParar()
    {
        File.WriteAllText(_arquivo, """{ "AlertRepetitions": 0 }""");

        var s = AppSettings.LoadFrom(_arquivo);

        Assert.Equal(0, s.AlertRepetitions);
    }

    [Fact]
    public void AtalhoAusenteNoArquivo_RecebeOPadrao()
    {
        File.WriteAllText(_arquivo, """
        { "Hotkeys": { "Zerar": "Ctrl+Alt+X" } }
        """);

        var s = AppSettings.LoadFrom(_arquivo);

        Assert.Equal("Ctrl+Alt+X", s.Hotkeys[nameof(HotkeyAction.Zerar)]);
        // As acoes que faltavam voltam com o padrao, sem quebrar o registro.
        Assert.Equal("Ctrl+Alt+S", s.Hotkeys[nameof(HotkeyAction.IniciarPausar)]);
        Assert.True(s.Hotkeys.ContainsKey(nameof(HotkeyAction.MostrarEsconder)));
    }
}
