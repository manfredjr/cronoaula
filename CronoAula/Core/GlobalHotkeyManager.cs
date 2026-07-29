using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace CronoAula.Core;

/// <summary>
/// Registra atalhos em nivel de sistema operacional, de modo que funcionem mesmo
/// quando o foco esta em outro programa (PowerPoint, navegador, Dev-C++).
///
/// Como funciona a interoperabilidade com a user32.dll:
///  - RegisterHotKey associa uma combinacao de teclas a um identificador numerico
///    e a uma janela. Enquanto o registro existir, o Windows entrega a mensagem
///    WM_HOTKEY aquela janela sempre que a combinacao for pressionada, seja qual
///    for o aplicativo em primeiro plano.
///  - Como o WPF nao expoe o laco de mensagens do Win32 diretamente, usamos
///    HwndSource para obter o handle (HWND) da janela e instalar um "hook" que
///    intercepta as mensagens brutas antes do WPF processa-las.
///  - O registro e exclusivo no sistema: se outro programa ja tomou a combinacao,
///    RegisterHotKey devolve false. Nesse caso avisamos o usuario em vez de
///    falhar em silencio.
///  - Todo atalho registrado precisa ser liberado com UnregisterHotKey ao sair,
///    senao a combinacao pode ficar presa ate o fim da sessao do Windows.
/// </summary>
public sealed class GlobalHotkeyManager : IDisposable
{
    // --- P/Invoke ---------------------------------------------------------

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>Mensagem que o Windows envia quando um atalho registrado e acionado.</summary>
    private const int WM_HOTKEY = 0x0312;

    // Modificadores aceitos por RegisterHotKey.
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    /// <summary>
    /// Evita a repeticao automatica enquanto a tecla fica pressionada: sem isso,
    /// segurar Ctrl+Alt+Up somaria dezenas de minutos de uma vez.
    /// </summary>
    private const uint MOD_NOREPEAT = 0x4000;

    // ----------------------------------------------------------------------

    private readonly Dictionary<int, HotkeyAction> _registered = new();
    private HwndSource? _source;
    private IntPtr _handle;
    private int _nextId = 9000; // faixa arbitraria, so precisa ser unica nesta janela
    private bool _disposed;

    /// <summary>Disparado na thread da interface quando um atalho e acionado.</summary>
    public event EventHandler<HotkeyAction>? HotkeyPressed;

    /// <summary>
    /// Liga o gerenciador a uma janela ja carregada. Deve ser chamado depois que a
    /// janela tem handle valido (por exemplo, no evento SourceInitialized ou Loaded).
    /// </summary>
    public void Attach(System.Windows.Window window)
    {
        _handle = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_handle);
        // Intercepta as mensagens do Win32 destinadas a esta janela.
        _source?.AddHook(WndProc);
    }

    /// <summary>
    /// Tenta registrar os atalhos descritos nas preferencias.
    /// Devolve a lista de combinacoes que falharam, com o motivo, para que a
    /// interface possa avisar o usuario de forma clara.
    /// </summary>
    public IReadOnlyList<string> RegisterAll(IDictionary<string, string> hotkeys)
    {
        UnregisterAll();
        var failures = new List<string>();

        foreach (var (actionName, combo) in hotkeys)
        {
            if (!Enum.TryParse<HotkeyAction>(actionName, out var action))
                continue;

            if (string.IsNullOrWhiteSpace(combo))
                continue; // atalho desativado de proposito

            if (!HotkeyCombo.TryParse(combo, out var mods, out var vk))
            {
                failures.Add($"{Describe(action)}: combinacao \"{combo}\" nao foi reconhecida.");
                continue;
            }

            var id = _nextId++;
            if (RegisterHotKey(_handle, id, mods | MOD_NOREPEAT, vk))
            {
                _registered[id] = action;
            }
            else
            {
                // Causa mais comum: outro programa ja registrou essa combinacao.
                failures.Add($"{Describe(action)}: \"{combo}\" ja esta em uso por outro programa.");
            }
        }

        return failures;
    }

    /// <summary>Libera todos os atalhos registrados.</summary>
    public void UnregisterAll()
    {
        if (_handle == IntPtr.Zero)
            return;

        foreach (var id in _registered.Keys)
            UnregisterHotKey(_handle, id);

        _registered.Clear();
    }

    /// <summary>
    /// Hook do laco de mensagens. Recebe toda mensagem enviada a janela; nos so
    /// nos interessamos por WM_HOTKEY, cujo wParam carrega o id do atalho.
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_registered.TryGetValue(id, out var action))
            {
                HotkeyPressed?.Invoke(this, action);
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private static string Describe(HotkeyAction action) => action switch
    {
        HotkeyAction.IniciarPausar => "Iniciar/Pausar",
        HotkeyAction.Zerar => "Zerar",
        HotkeyAction.AdicionarMinuto => "Somar 1 minuto",
        HotkeyAction.SubtrairMinuto => "Subtrair 1 minuto",
        HotkeyAction.MostrarEsconder => "Mostrar/Esconder",
        _ => action.ToString()
    };

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        UnregisterAll();
        _source?.RemoveHook(WndProc);
        _source = null;
    }
}

/// <summary>
/// Converte texto como "Ctrl+Alt+S" nos codigos numericos que a API do Windows
/// espera (mascara de modificadores + virtual-key code).
/// </summary>
public static class HotkeyCombo
{
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    public static bool TryParse(string? combo, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;

        if (string.IsNullOrWhiteSpace(combo))
            return false;

        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return false;

        string? keyPart = null;

        foreach (var raw in parts)
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl" or "control" or "controle":
                    modifiers |= MOD_CONTROL;
                    break;
                case "alt":
                    modifiers |= MOD_ALT;
                    break;
                case "shift":
                    modifiers |= MOD_SHIFT;
                    break;
                case "win" or "windows":
                    modifiers |= MOD_WIN;
                    break;
                default:
                    if (keyPart is not null)
                        return false; // mais de uma tecla principal
                    keyPart = raw;
                    break;
            }
        }

        if (keyPart is null)
            return false;

        // O Windows exige pelo menos um modificador para atalhos globais.
        if (modifiers == 0)
            return false;

        return TryParseKey(keyPart, out virtualKey);
    }

    private static bool TryParseKey(string key, out uint vk)
    {
        vk = 0;
        var k = key.ToLowerInvariant();

        // Letras A-Z: o virtual-key code coincide com o ASCII maiusculo.
        if (k.Length == 1 && k[0] is >= 'a' and <= 'z')
        {
            vk = (uint)char.ToUpperInvariant(k[0]);
            return true;
        }

        // Digitos 0-9: idem.
        if (k.Length == 1 && k[0] is >= '0' and <= '9')
        {
            vk = k[0];
            return true;
        }

        // Teclas F1-F12 => VK_F1 (0x70) em diante.
        if (k.Length is 2 or 3 && k[0] == 'f' && int.TryParse(k[1..], out var fn) && fn is >= 1 and <= 12)
        {
            vk = (uint)(0x70 + fn - 1);
            return true;
        }

        vk = k switch
        {
            "up" or "cima" => 0x26,        // VK_UP
            "down" or "baixo" => 0x28,     // VK_DOWN
            "left" or "esquerda" => 0x25,  // VK_LEFT
            "right" or "direita" => 0x27,  // VK_RIGHT
            "space" or "espaco" => 0x20,   // VK_SPACE
            "esc" or "escape" => 0x1B,     // VK_ESCAPE
            "home" => 0x24,
            "end" => 0x23,
            "pgup" or "pageup" => 0x21,
            "pgdn" or "pagedown" => 0x22,
            "insert" or "ins" => 0x2D,
            "delete" or "del" => 0x2E,
            _ => 0
        };

        return vk != 0;
    }
}
