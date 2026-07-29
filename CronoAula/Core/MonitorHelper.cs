using System.Runtime.InteropServices;

namespace CronoAula.Core;

/// <summary>
/// Um monitor conectado, em pixels fisicos.
/// </summary>
/// <param name="Id">Nome do dispositivo (ex.: "\\.\DISPLAY1"). Usado para lembrar a escolha.</param>
/// <param name="Rotulo">Texto amigavel para o menu (ex.: "Monitor 2 (1920x1080)").</param>
public sealed record MonitorInfo(
    string Id,
    string Rotulo,
    int Left,
    int Top,
    int Width,
    int Height,
    bool Principal);

/// <summary>
/// Lista os monitores conectados via user32.dll.
///
/// Por que nao usar System.Windows.Forms.Screen: isso obrigaria a referenciar o
/// WinForms inteiro so para ler quatro numeros, engordando o executavel unico.
///
/// Os valores retornados sao pixels FISICOS, que e exatamente o que
/// SetWindowPos espera. Assim posicionamos a janela em tela cheia sem precisar
/// converter unidades de DPI do WPF, que e onde esse tipo de codigo costuma
/// errar em telas com escalas diferentes.
/// </summary>
public static class MonitorHelper
{
    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFOEX info);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    /// <summary>Se a janela nao estiver em nenhum monitor, devolve o mais proximo.</summary>
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    private const int MONITORINFOF_PRIMARY = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;   // area total do monitor
        public RECT rcWork;      // area util (descontando a barra de tarefas)
        public int dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    /// <summary>Todos os monitores conectados, na ordem em que o Windows os reporta.</summary>
    public static IReadOnlyList<MonitorInfo> Listar()
    {
        var encontrados = new List<MonitorInfo>();

        var handles = new List<IntPtr>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            (IntPtr h, IntPtr _, ref RECT _, IntPtr _) => { handles.Add(h); return true; },
            IntPtr.Zero);

        var indice = 1;
        foreach (var h in handles)
        {
            var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            if (!GetMonitorInfoW(h, ref info))
                continue;

            var r = info.rcMonitor;
            var largura = r.Right - r.Left;
            var altura = r.Bottom - r.Top;
            var principal = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;

            var rotulo = $"Monitor {indice} ({largura}x{altura})"
                         + (principal ? ", principal" : "");

            encontrados.Add(new MonitorInfo(
                info.szDevice, rotulo, r.Left, r.Top, largura, altura, principal));

            indice++;
        }

        return encontrados;
    }

    /// <summary>Monitor que contem a janela indicada (ou o mais proximo dela).</summary>
    public static MonitorInfo? DoHandle(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return Listar().FirstOrDefault(m => m.Principal) ?? Listar().FirstOrDefault();

        var h = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
        if (!GetMonitorInfoW(h, ref info))
            return Listar().FirstOrDefault();

        return Listar().FirstOrDefault(m => m.Id == info.szDevice)
               ?? Listar().FirstOrDefault();
    }

    /// <summary>
    /// Resolve qual monitor usar: o salvo nas preferencias, se ainda existir;
    /// senao, o que contem a janela. Cobre o caso do projetor desconectado.
    /// </summary>
    public static MonitorInfo? Resolver(string? idSalvo, IntPtr hwnd)
    {
        if (!string.IsNullOrWhiteSpace(idSalvo))
        {
            var salvo = Listar().FirstOrDefault(m => m.Id == idSalvo);
            if (salvo is not null)
                return salvo;
        }

        return DoHandle(hwnd);
    }
}
