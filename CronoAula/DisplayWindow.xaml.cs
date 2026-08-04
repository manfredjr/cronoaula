using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using CronoAula.Core;

namespace CronoAula;

/// <summary>
/// Janela de exibição do tempo, usada no modo tela cheia.
///
/// Ela mostra apenas o relógio. Quem comanda continua sendo a janela principal,
/// que fica na outra tela funcionando como painel de controle. As duas leem o
/// mesmo cronômetro, então não existe estado duplicado para sincronizar.
///
/// Com um monitor só não há outra tela para o painel, então esta janela ganha
/// controles próprios, que aparecem ao mover o mouse e somem sozinhos.
/// </summary>
public partial class DisplayWindow : Window
{
    /// <summary>
    /// Reposiciona a janela em pixels físicos, sem passar pela conversão de
    /// unidades do WPF. É o caminho que não erra quando o notebook e o projetor
    /// têm escalas de DPI diferentes, situação comum em sala de aula.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private readonly DispatcherTimer _ocultarControles =
        new() { Interval = TimeSpan.FromSeconds(3) };

    private Storyboard? _piscar;

    /// <summary>Disparado quando o usuário pede para sair da tela cheia.</summary>
    public event EventHandler? PedidoDeSaida;

    /// <summary>Ações acionadas pelos controles de emergência desta janela.</summary>
    public event EventHandler? PedidoIniciarPausar;
    public event EventHandler? PedidoZerar;
    public event EventHandler? PedidoMaisUmMinuto;

    public DisplayWindow()
    {
        InitializeComponent();

        _ocultarControles.Tick += (_, _) =>
        {
            _ocultarControles.Stop();
            AnimarOpacidade(Controles, 0.0);
        };

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key is Key.Escape or Key.F11)
            {
                PedidoDeSaida?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        };

        MouseDoubleClick += (_, _) => PedidoDeSaida?.Invoke(this, EventArgs.Empty);
        MouseMove += (_, _) => MostrarControles();

        Closed += (_, _) =>
        {
            _ocultarControles.Stop();
            PararPiscar();
        };
    }

    /// <summary>
    /// Coloca a janela ocupando o monitor indicado.
    /// </summary>
    /// <param name="comControlesProprios">
    /// True quando não existe painel de controle em outra tela, ou seja, quando
    /// só há um monitor. Nesse caso a janela exibe seus próprios botões e recebe
    /// o foco, para que Esc funcione.
    /// </param>
    public void OcuparMonitor(MonitorInfo monitor, bool comControlesProprios)
    {
        // ShowActivated só é respeitado antes de a janela aparecer.
        ShowActivated = comControlesProprios;
        Show();

        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            var flags = SWP_SHOWWINDOW | (comControlesProprios ? 0 : SWP_NOACTIVATE);
            SetWindowPos(hwnd, HWND_TOPMOST,
                monitor.Left, monitor.Top, monitor.Width, monitor.Height, flags);
        }

        Controles.Visibility = comControlesProprios ? Visibility.Visible : Visibility.Collapsed;

        if (comControlesProprios)
        {
            MostrarControles();
            Activate();
        }
    }

    /// <summary>Atualiza o relógio exibido. Chamado pela janela principal.</summary>
    public void Atualizar(string texto, Brush cor, string estado, bool piscando, string rotuloPrimario)
    {
        Mostrador.Text = texto;
        Mostrador.Foreground = cor;
        RotuloEstado.Text = estado;
        BotaoPrimario.Content = rotuloPrimario;

        if (piscando)
            IniciarPiscar();
        else
            PararPiscar();
    }

    private void MostrarControles()
    {
        if (Controles.Visibility != Visibility.Visible)
            return;

        AnimarOpacidade(Controles, 1.0);
        _ocultarControles.Stop();
        _ocultarControles.Start();
    }

    private static void AnimarOpacidade(UIElement alvo, double destino)
    {
        alvo.BeginAnimation(OpacityProperty, new DoubleAnimation
        {
            To = destino,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
    }

    /// <summary>
    /// Pisca suave: um esmaecimento de ida e volta. Escolhido em vez de
    /// liga/desliga abrupto para não competir com a atenção da turma.
    /// </summary>
    private void IniciarPiscar()
    {
        if (_piscar is not null)
            return;

        var animacao = new DoubleAnimation
        {
            From = 1.0,
            To = 0.35,
            Duration = TimeSpan.FromSeconds(0.9),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        _piscar = new Storyboard();
        _piscar.Children.Add(animacao);
        Storyboard.SetTarget(animacao, Mostrador);
        Storyboard.SetTargetProperty(animacao, new PropertyPath(OpacityProperty));
        _piscar.Begin();
    }

    private void PararPiscar()
    {
        if (_piscar is null)
            return;

        _piscar.Stop();
        _piscar = null;
        Mostrador.Opacity = 1.0;
    }

    private void BotaoPrimario_Click(object sender, RoutedEventArgs e)
    {
        PedidoIniciarPausar?.Invoke(this, EventArgs.Empty);
        MostrarControles();
    }

    private void BotaoZerar_Click(object sender, RoutedEventArgs e)
    {
        PedidoZerar?.Invoke(this, EventArgs.Empty);
        MostrarControles();
    }

    private void BotaoMaisUm_Click(object sender, RoutedEventArgs e)
    {
        PedidoMaisUmMinuto?.Invoke(this, EventArgs.Empty);
        MostrarControles();
    }

    private void BotaoSair_Click(object sender, RoutedEventArgs e) =>
        PedidoDeSaida?.Invoke(this, EventArgs.Empty);
}
