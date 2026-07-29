using CronoAula.Core;

namespace CronoAula.Tests;

public class HotkeyComboTests
{
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;

    [Fact]
    public void PadraoCtrlAltS()
    {
        Assert.True(HotkeyCombo.TryParse("Ctrl+Alt+S", out var mods, out var vk));
        Assert.Equal(MOD_CONTROL | MOD_ALT, mods);
        Assert.Equal((uint)'S', vk);
    }

    [Fact]
    public void SetasSaoReconhecidas()
    {
        Assert.True(HotkeyCombo.TryParse("Ctrl+Alt+Up", out _, out var cima));
        Assert.Equal(0x26u, cima); // VK_UP

        Assert.True(HotkeyCombo.TryParse("Ctrl+Alt+Down", out _, out var baixo));
        Assert.Equal(0x28u, baixo); // VK_DOWN
    }

    [Fact]
    public void TeclasDeFuncao()
    {
        Assert.True(HotkeyCombo.TryParse("Ctrl+Shift+F9", out var mods, out var vk));
        Assert.Equal(MOD_CONTROL | MOD_SHIFT, mods);
        Assert.Equal(0x78u, vk); // VK_F9 = 0x70 + 8
    }

    [Fact]
    public void NomesEmPortuguesTambemFuncionam()
    {
        Assert.True(HotkeyCombo.TryParse("Controle+Alt+Cima", out var mods, out var vk));
        Assert.Equal(MOD_CONTROL | MOD_ALT, mods);
        Assert.Equal(0x26u, vk);
    }

    [Fact]
    public void NaoDiferenciaMaiusculas()
    {
        Assert.True(HotkeyCombo.TryParse("ctrl+alt+s", out var a, out var x));
        Assert.True(HotkeyCombo.TryParse("CTRL+ALT+S", out var b, out var y));
        Assert.Equal(a, b);
        Assert.Equal(x, y);
    }

    [Theory]
    [InlineData("S")]              // sem modificador: o Windows exige ao menos um
    [InlineData("Ctrl+")]          // sem tecla principal
    [InlineData("Ctrl+Alt")]       // so modificadores
    [InlineData("Ctrl+Alt+S+R")]   // duas teclas principais
    [InlineData("Ctrl+Alt+Foo")]   // tecla desconhecida
    [InlineData("")]
    [InlineData(null)]
    public void CombinacoesInvalidas(string? combo)
    {
        Assert.False(HotkeyCombo.TryParse(combo, out _, out _));
    }

    [Fact]
    public void TodosOsAtalhosPadraoSaoValidos()
    {
        // Protege contra um padrao quebrado passar despercebido para o usuario.
        foreach (var (_, combo) in new AppSettings().Hotkeys)
            Assert.True(HotkeyCombo.TryParse(combo, out _, out _), $"Atalho padrao invalido: {combo}");
    }
}
