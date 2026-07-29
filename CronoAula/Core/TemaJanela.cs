using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CronoAula.Core;

/// <summary>
/// Deixa a barra de titulo das janelas comuns (Preferencias, Sobre) escura,
/// combinando com o conteudo.
///
/// A barra de titulo e desenhada pelo Windows, nao pelo WPF, entao nao adianta
/// pintar pelo XAML: e preciso avisar o gerenciador de janelas (DWM) que a
/// janela usa tema escuro.
/// </summary>
public static class TemaJanela
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int valor, int tamanho);

    /// <summary>
    /// Codigo do atributo a partir do Windows 10 versao 2004. Compilacoes
    /// anteriores usavam 19; tentamos os dois e ignoramos a falha, porque uma
    /// barra de titulo clara e um detalhe estetico, nao um erro.
    /// </summary>
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_ANTIGO = 19;

    /// <summary>
    /// Aplica o tema escuro a barra de titulo. Chame no construtor da janela;
    /// o efeito e aplicado assim que ela ganha um handle.
    /// </summary>
    public static void UsarBarraEscura(Window janela)
    {
        janela.SourceInitialized += (_, _) =>
        {
            try
            {
                var hwnd = new WindowInteropHelper(janela).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                var ligado = 1;
                if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref ligado, sizeof(int)) != 0)
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_ANTIGO, ref ligado, sizeof(int));
            }
            catch
            {
                // Versao de Windows sem suporte: segue com a barra clara.
            }
        };
    }
}
