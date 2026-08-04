using System.Runtime.InteropServices;
using System.Text;

namespace CronoAula.Core;

/// <summary>
/// Testa, uma a uma, se as combinacoes configuradas estao livres no sistema.
///
/// Serve para responder a pergunta que aparece quando um atalho "para de
/// funcionar": o problema e do CronoAula ou outro programa tomou a tecla?
/// Roda com "CronoAula.exe --atalhos".
///
/// O teste registra e libera cada combinacao na hora. Por isso deve rodar com o
/// cronometro fechado: com ele aberto, as combinacoes aparecerao como ocupadas
/// pelo proprio CronoAula.
/// </summary>
public static class DiagnosticoAtalhos
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_NOREPEAT = 0x4000;

    public static string Gerar(AppSettings settings)
    {
        var texto = new StringBuilder();
        texto.AppendLine("Situação das combinações de teclas neste computador:");
        texto.AppendLine();

        var id = 31000;
        var ocupadas = 0;

        foreach (var acao in Enum.GetValues<HotkeyAction>())
        {
            settings.Hotkeys.TryGetValue(acao.ToString(), out var combo);
            combo = combo?.Trim() ?? "";

            var nome = GlobalHotkeyManager.Describe(acao);

            if (string.IsNullOrWhiteSpace(combo))
            {
                texto.AppendLine($"{nome}: desligado nas preferências");
                continue;
            }

            if (!HotkeyCombo.TryParse(combo, out var mods, out var vk))
            {
                texto.AppendLine($"{nome}: \"{combo}\" não é uma combinação válida");
                ocupadas++;
                continue;
            }

            // Registro temporario, so para descobrir se a combinacao esta livre.
            if (RegisterHotKey(IntPtr.Zero, id, mods | MOD_NOREPEAT, vk))
            {
                UnregisterHotKey(IntPtr.Zero, id);
                texto.AppendLine($"{nome}: {combo} está livre");
            }
            else
            {
                texto.AppendLine($"{nome}: {combo} está OCUPADA por outro programa");
                ocupadas++;
            }

            id++;
        }

        texto.AppendLine();

        if (ocupadas == 0)
        {
            texto.AppendLine("Todas as combinações estão livres.");
            texto.AppendLine();
            texto.AppendLine("Se mesmo assim um atalho não responde, confira se o CronoAula");
            texto.AppendLine("já não está aberto e escondido. Nesse caso, abrir o programa de");
            texto.AppendLine("novo traz a janela existente de volta.");
        }
        else
        {
            texto.AppendLine($"{ocupadas} combinação(ões) estão indisponíveis.");
            texto.AppendLine();
            texto.AppendLine("Drivers de vídeo costumam reservar Ctrl+Alt com as setas.");
            texto.AppendLine("Abra as Preferências do CronoAula e escolha outras combinações,");
            texto.AppendLine("como Ctrl+Shift+F9 ou Ctrl+Alt+PageUp.");
        }

        texto.AppendLine();
        texto.AppendLine("Feche o CronoAula antes de rodar este teste. Com ele aberto, as");
        texto.AppendLine("combinações aparecem ocupadas pelo próprio programa.");

        return texto.ToString();
    }
}
