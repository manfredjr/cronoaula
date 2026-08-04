using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using CronoAula.Core;
using CronoAula.ViewModels;

namespace CronoAula;

public partial class MainWindow : Window
{
    // ------------------------------------------------------------------
    // Interoperabilidade com a user32.dll
    // ------------------------------------------------------------------

    /// <summary>
    /// Reposiciona a janela na ordem Z. Usamos isto para reafirmar periodicamente
    /// que a janela e "topmost".
    ///
    /// Por que isso e necessario: definir Topmost=true no WPF nao basta contra o
    /// modo de apresentacao do PowerPoint. O slideshow tambem se declara topmost e,
    /// ao entrar em tela cheia, passa a frente de quem ja estava la. Reafirmar a
    /// posicao a cada poucos segundos devolve o cronometro para o topo.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;

    /// <summary>
    /// Crucial: reposiciona sem dar foco a janela. Sem esta flag, o cronometro
    /// roubaria o foco do PowerPoint a cada reafirmacao e travaria a passagem
    /// de slides pelo teclado.
    /// </summary>
    private const uint SWP_NOACTIVATE = 0x0010;

    /// <summary>Usado ao reposicionar a janela entre monitores.</summary>
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>Retangulo do Win32, em pixels fisicos.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    // ------------------------------------------------------------------
    // Estado da tela cheia
    // ------------------------------------------------------------------

    /// <summary>
    /// Geometria da janela antes de entrar em tela cheia, para restaurar ao sair.
    /// Null quando o modo normal esta ativo.
    /// </summary>
    private (double Left, double Top, double Opacity)? _antesDaTelaCheia;

    public bool EmTelaCheia => _antesDaTelaCheia is not null;

    /// <summary>
    /// Janela que projeta o relogio para a turma. Existe apenas no modo tela
    /// cheia; fora dele fica null.
    /// </summary>
    private DisplayWindow? _exibicao;

    // ------------------------------------------------------------------

    private readonly AppSettings _settings;
    private readonly MainViewModel _vm;
    private readonly SoundService _sound = new();
    private readonly GlobalHotkeyManager _hotkeys = new();

    /// <summary>Atualiza o mostrador enquanto conta. Parado quando nao ha contagem.</summary>
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(100) };

    /// <summary>Reafirma a posicao no topo, para vencer apresentacoes em tela cheia.</summary>
    private readonly DispatcherTimer _reforcoTopo = new() { Interval = TimeSpan.FromSeconds(2) };

    private Storyboard? _piscar;
    private bool _mouseSobreJanela;
    private bool _encerrando;

    /// <summary>
    /// Cor de cada faixa de alerta, vinda do dicionario da marca. Ler dali, e
    /// nao fixar valores aqui, mantem um unico lugar de verdade para a paleta.
    /// </summary>
    private Brush CorDaFaixa(AlertLevel nivel) => nivel switch
    {
        AlertLevel.Atencao => (Brush)FindResource("AlertaAtencao"),
        AlertLevel.Urgente => (Brush)FindResource("AlertaUrgente"),
        AlertLevel.Estourado => (Brush)FindResource("AlertaEstourado"),
        _ => (Brush)FindResource("AlertaNormal")
    };

    public MainWindow()
    {
        InitializeComponent();

        _settings = AppSettings.Load();
        _vm = new MainViewModel(_settings, _sound);
        _vm.PropertyChanged += ViewModel_PropertyChanged;

        _tick.Tick += (_, _) => _vm.Tick();
        _reforcoTopo.Tick += (_, _) => ReafirmarTopo();

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
        MouseWheel += MainWindow_MouseWheel;
        // Preview (tunelamento) em vez de KeyDown: garante que F11 e Esc cheguem
        // aqui mesmo quando o foco esta na caixa de tempo personalizado.
        PreviewKeyDown += MainWindow_KeyDown;
        MouseDoubleClick += MainWindow_MouseDoubleClick;
        MouseEnter += (_, _) => { _mouseSobreJanela = true; AplicarOpacidade(); };
        MouseLeave += (_, _) => { _mouseSobreJanela = false; AplicarOpacidade(); };

        // O item de tela cheia depende de quantos monitores existem no momento,
        // entao e remontado toda vez que o menu abre.
        ContextMenuOpening += (_, _) => PrepararMenuTelaCheia();
    }

    // ------------------------------------------------------------------
    // Ciclo de vida
    // ------------------------------------------------------------------

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ConstruirPresets();
        AplicarTamanho(_settings.Size);
        AplicarOpacidade();

        Topmost = _settings.AlwaysOnTop;
        ShowInTaskbar = _settings.ShowInTaskbar;
        MenuSempreTopo.IsChecked = _settings.AlwaysOnTop;
        MenuBarraTarefas.IsChecked = _settings.ShowInTaskbar;

        RestaurarPosicao();
        AtualizarMostrador();

        if (_settings.AlwaysOnTop)
            _reforcoTopo.Start();

        // Os atalhos globais so podem ser registrados depois que a janela tem HWND.
        _hotkeys.Attach(this);
        _hotkeys.HotkeyPressed += Hotkeys_Pressed;
        RegistrarAtalhos(avisar: true);
    }

    /// <summary>
    /// Situacao atual dos atalhos globais, mostrada nas Preferencias.
    /// </summary>
    public IReadOnlyList<HotkeyStatus> EstadoDosAtalhos => _hotkeys.UltimoEstado;

    /// <summary>
    /// (Re)registra os atalhos globais. Com <paramref name="avisar"/>, exibe uma
    /// mensagem se alguma combinacao estiver disputada.
    /// </summary>
    public void RegistrarAtalhos(bool avisar)
    {
        var estado = _hotkeys.RegisterAll(_settings.Hotkeys);
        var problemas = estado.Where(s => s.Problema).ToList();

        if (!avisar || problemas.Count == 0)
            return;

        var lista = string.Join("\n",
            problemas.Select(s => $"  {s.Descricao}: {s.Combo} ({s.Resumo})"));

        MessageBox.Show(
            "Estes atalhos não estão valendo:\n\n" + lista
            + "\n\nOutro programa costuma ser o motivo. Drivers de vídeo, por "
            + "exemplo, costumam reservar Ctrl+Alt com as setas.\n\n"
            + "Abra as Preferências para escolher outras combinações. Lá você "
            + "também vê quais atalhos estão funcionando.",
            "CronoAula - atalhos em uso",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    /// <summary>
    /// Liga o aviso vindo de uma segunda copia do programa a esta janela.
    /// Em vez de abrir outra, trazemos esta de volta.
    /// </summary>
    public void EscutarSegundaCopia(InstanciaUnica instancia)
    {
        instancia.PedidoDeMostrarJanela += (_, _) =>
        {
            // O aviso chega em uma thread de vigilancia; mexer em controles
            // exige voltar para a thread da interface.
            Dispatcher.Invoke(MostrarJanela);
        };
    }

    /// <summary>Traz a janela de volta, esteja ela escondida ou apenas atras de outra.</summary>
    private void MostrarJanela()
    {
        if (!IsVisible)
        {
            Show();
            RestaurarPosicao();
        }

        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;

        Activate();
        ReafirmarTopo();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _encerrando = true;
        SalvarPreferencias();

        // A janela de exibicao nao pode sobreviver ao painel de controle.
        _exibicao?.Close();
        _exibicao = null;

        _hotkeys.Dispose();
        _sound.Dispose();   // encerra a repeticao do alerta, se estiver tocando
        _tick.Stop();
        _reforcoTopo.Stop();
    }

    private void SalvarPreferencias()
    {
        if (_antesDaTelaCheia is { } anterior)
        {
            // Fechar durante a tela cheia nao pode gravar a geometria da tela
            // inteira como posicao da janelinha: guardamos onde ela estava antes.
            _settings.Left = anterior.Left;
            _settings.Top = anterior.Top;
            _settings.Opacity = anterior.Opacity;
        }
        else if (IsVisible)
        {
            // Se a janela estiver escondida, mantem a ultima posicao conhecida.
            _settings.Left = Left;
            _settings.Top = Top;
        }

        _settings.Save();
    }

    // ------------------------------------------------------------------
    // Posicionamento
    // ------------------------------------------------------------------

    private void RestaurarPosicao()
    {
        if (_settings.Left is { } l && _settings.Top is { } t && EstaVisivelNaTela(l, t))
        {
            Left = l;
            Top = t;
            return;
        }

        // Primeira execucao (ou monitor que sumiu): canto inferior direito.
        MoverParaCanto("ID");
    }

    /// <summary>
    /// Confere se a posicao salva ainda cai dentro de algum monitor. Evita que a
    /// janela reapareca fora da tela depois que o professor desconecta o projetor.
    /// </summary>
    private bool EstaVisivelNaTela(double left, double top)
    {
        var area = SystemParameters.VirtualScreenWidth > 0
            ? new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                       SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight)
            : SystemParameters.WorkArea;

        // Exige que uma parte razoavel da janela esteja dentro da area visivel.
        var janela = new Rect(left, top, Math.Max(ActualWidth, 80), Math.Max(ActualHeight, 40));
        var interseccao = Rect.Intersect(area, janela);
        return interseccao != Rect.Empty && interseccao.Width > 40 && interseccao.Height > 20;
    }

    private void MoverParaCanto(string canto)
    {
        const double margem = 20;
        var area = SystemParameters.WorkArea;

        // Garante que a largura/altura ja estejam calculadas.
        UpdateLayout();
        var w = ActualWidth;
        var h = ActualHeight;

        (Left, Top) = canto switch
        {
            "SE" => (area.Left + margem, area.Top + margem),
            "SD" => (area.Right - w - margem, area.Top + margem),
            "IE" => (area.Left + margem, area.Bottom - h - margem),
            _ => (area.Right - w - margem, area.Bottom - h - margem) // ID
        };
    }

    /// <summary>Reafirma a posicao no topo sem roubar o foco do aplicativo ativo.</summary>
    private void ReafirmarTopo()
    {
        if (!_settings.AlwaysOnTop || !IsVisible || _encerrando)
            return;

        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    // ------------------------------------------------------------------
    // Aparencia
    // ------------------------------------------------------------------

    private void ConstruirPresets()
    {
        PainelPresets.Children.Clear();

        foreach (var minutos in _settings.Presets)
        {
            var botao = new Button
            {
                Content = minutos.ToString(),
                Style = (Style)FindResource("BotaoPreset"),
                Tag = minutos,
                ToolTip = $"{minutos} minutos. Clique uma vez para carregar, duas para iniciar."
            };
            botao.Click += BotaoPreset_Click;
            PainelPresets.Children.Add(botao);
        }
    }

    private void AplicarTamanho(DisplaySize tamanho)
    {
        // Os tamanhos mudam SO os digitos. Antes existia um ScaleTransform que
        // ampliava a janela inteira, botoes junto; nao fazia sentido, porque um
        // botao e alvo de clique e ja nasce num tamanho confortavel. O que muda
        // com a distancia do monitor e a legibilidade do relogio, nao a do botao.
        Mostrador.FontSize = tamanho switch
        {
            DisplaySize.Pequeno => 32,
            DisplaySize.Grande => 78,
            _ => 46
        };

        _settings.Size = tamanho;
        MenuTamPequeno.IsChecked = tamanho == DisplaySize.Pequeno;
        MenuTamMedio.IsChecked = tamanho == DisplaySize.Medio;
        MenuTamGrande.IsChecked = tamanho == DisplaySize.Grande;
    }

    /// <summary>
    /// Com o mouse sobre a janela ela volta a 100% para facilitar o clique;
    /// fora dela, volta a opacidade escolhida.
    /// </summary>
    private void AplicarOpacidade()
    {
        Opacity = _mouseSobreJanela ? 1.0 : _settings.Opacity;
    }

    private void AtualizarMostrador()
    {
        Mostrador.Text = _vm.Display;
        BotaoPrimario.Content = _vm.PrimaryButtonLabel;

        var cor = CorDaFaixa(_vm.Alert);
        Mostrador.Foreground = cor;

        var estado = DescreverEstado();
        Estado.Text = estado;

        // Espelha no relogio projetado para a turma, quando ele existe.
        _exibicao?.Atualizar(_vm.Display, cor, estado,
            _vm.Alert == AlertLevel.Estourado, _vm.PrimaryButtonLabel);

        DestacarPresetArmado();

        if (_vm.Alert == AlertLevel.Estourado)
            IniciarPiscar();
        else
            PararPiscar();
    }

    /// <summary>
    /// Texto que acompanha a cor do mostrador.
    ///
    /// O manual da marca proibe indicar um estado apenas por cor. Como as
    /// faixas de atencao e urgencia sao tons vizinhos de laranja, e como parte
    /// da turma pode nao distinguir bem essas cores, cada faixa recebe um nome
    /// escrito.
    /// </summary>
    private string DescreverEstado()
    {
        if (_vm.Engine.State == TimerState.Paused)
            return "pausado";

        if (_vm.Engine.IsOvertime)
            return "tempo excedido";

        if (_vm.ArmedPreset is { } p)
            return $"{p} min carregado. Clique de novo para iniciar";

        // A cor do mostrador vale tambem com o cronometro parado, entao o nome
        // da faixa precisa aparecer nos dois casos. Do contrario o estado
        // ficaria indicado apenas por cor, o que o manual da marca proibe.
        var faixa = _vm.Alert switch
        {
            AlertLevel.Atencao => "reta final",
            AlertLevel.Urgente => "último minuto",
            _ => ""
        };

        if (faixa.Length > 0)
            return faixa;

        return _vm.Engine.State == TimerState.Running ? "" : "pronto";
    }

    private void DestacarPresetArmado()
    {
        foreach (var filho in PainelPresets.Children)
        {
            if (filho is not Button b || b.Tag is not int minutos)
                continue;

            var armado = _vm.ArmedPreset == minutos;
            b.Background = (Brush)FindResource(armado ? "PresetArmado" : "BotaoFundo");
            b.Foreground = (Brush)FindResource(armado ? "PresetArmadoTexto" : "BotaoTexto");
        }
    }

    /// <summary>
    /// Pisca suave: um fade de ida e volta na opacidade do texto. Escolhido em vez
    /// de liga/desliga abrupto para nao competir com a atencao da turma.
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

    // ------------------------------------------------------------------
    // Tela cheia
    // ------------------------------------------------------------------

    /// <summary>
    /// Alterna entre o modo normal e a tela cheia. Usada pelo menu, pelo F11,
    /// pelo duplo clique e pelo atalho global.
    /// </summary>
    public void AlternarTelaCheia(string? idMonitor = null)
    {
        if (EmTelaCheia)
            SairTelaCheia();
        else
            EntrarTelaCheia(idMonitor);
    }

    private void EntrarTelaCheia(string? idMonitor)
    {
        if (EmTelaCheia)
            return;

        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var monitores = MonitorHelper.Listar();
        var daExibicao = MonitorHelper.Resolver(idMonitor ?? _settings.FullscreenMonitor, hwnd);
        if (daExibicao is null)
            return;

        if (idMonitor is not null)
            _settings.FullscreenMonitor = idMonitor;

        // Guarda a posicao para devolver a janela ao lugar de origem ao sair.
        _antesDaTelaCheia = (Left, Top, _settings.Opacity);

        // Com mais de um monitor, esta janela continua visivel na outra tela e
        // vira o painel de controle. Com um monitor so, nao ha para onde manda-la
        // sem cobrir a projecao, entao ela se esconde e a janela de exibicao
        // ganha seus proprios botoes.
        var doPainel = MonitorHelper.EscolherMonitorDoPainel(monitores, daExibicao);

        _exibicao = new DisplayWindow();
        _exibicao.PedidoDeSaida += (_, _) => SairTelaCheia();
        _exibicao.PedidoIniciarPausar += (_, _) => _vm.ToggleStartPause();
        _exibicao.PedidoZerar += (_, _) => _vm.Reset();
        _exibicao.PedidoMaisUmMinuto += (_, _) => _vm.AddMinutes(1);
        _exibicao.OcuparMonitor(daExibicao, comControlesProprios: doPainel is null);

        if (doPainel is null)
        {
            Hide();
        }
        else
        {
            // Painel sempre com opacidade total: aqui o professor precisa
            // enxergar e clicar, nao ficar discreto.
            _settings.Opacity = 1.0;
            AplicarOpacidade();
            MoverParaMonitor(doPainel);
            ReafirmarTopo();
        }

        AtualizarMostrador();
    }

    private void SairTelaCheia()
    {
        if (_antesDaTelaCheia is not { } anterior)
            return;

        _antesDaTelaCheia = null;

        if (_exibicao is not null)
        {
            _exibicao.Close();
            _exibicao = null;
        }

        if (!IsVisible)
            Show();

        // Devolve posicao e opacidade que a janela tinha antes.
        Left = anterior.Left;
        Top = anterior.Top;
        _settings.Opacity = anterior.Opacity;
        AplicarOpacidade();
        ReafirmarTopo();
        AtualizarMostrador();
    }

    /// <summary>
    /// Leva o painel de controle para o canto inferior direito do monitor
    /// indicado. Trabalha em pixels fisicos, pelo mesmo motivo da janela de
    /// exibicao: monitores com escalas de DPI diferentes.
    /// </summary>
    private void MoverParaMonitor(MonitorInfo monitor)
    {
        UpdateLayout();

        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        if (!GetWindowRect(hwnd, out var atual))
            return;

        var largura = atual.Right - atual.Left;
        var altura = atual.Bottom - atual.Top;
        var (left, top) = MonitorHelper.CantoInferiorDireito(monitor, largura, altura);

        SetWindowPos(hwnd, HWND_TOPMOST, left, top, 0, 0,
            SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    /// <summary>
    /// Monta o item "Tela cheia" do menu. Com um monitor so, e um item simples;
    /// com projetor conectado, vira um submenu para escolher a tela.
    /// </summary>
    private void PrepararMenuTelaCheia()
    {
        MenuTelaCheia.Items.Clear();

        if (EmTelaCheia)
        {
            MenuTelaCheia.Header = "Sair da tela cheia (Esc)";
            return;
        }

        var monitores = MonitorHelper.Listar();

        if (monitores.Count <= 1)
        {
            // Uma tela: clicar no proprio item ja aciona.
            MenuTelaCheia.Header = "Tela cheia (F11)";
            return;
        }

        MenuTelaCheia.Header = "Tela cheia em";
        foreach (var m in monitores)
        {
            var item = new MenuItem
            {
                Header = m.Rotulo,
                Tag = m.Id,
                IsCheckable = true,
                IsChecked = m.Id == _settings.FullscreenMonitor
            };
            item.Click += (s, _) =>
            {
                if (s is MenuItem { Tag: string id })
                    AlternarTelaCheia(id);
            };
            MenuTelaCheia.Items.Add(item);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // O relogio da interface so roda enquanto ha contagem: parado, o consumo
        // de CPU fica proximo de zero.
        if (e.PropertyName == nameof(MainViewModel.IsRunning))
        {
            if (_vm.IsRunning)
                _tick.Start();
            else
                _tick.Stop();
        }

        AtualizarMostrador();
    }

    // ------------------------------------------------------------------
    // Interacao com o mouse
    // ------------------------------------------------------------------

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F11:
                AlternarTelaCheia();
                e.Handled = true;
                break;

            case Key.Escape when EmTelaCheia:
                SairTelaCheia();
                e.Handled = true;
                break;
        }
    }

    private void MainWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Duplo clique sobre area vazia alterna a tela cheia. Sobre um botao,
        // nao: la o duplo clique e apenas dois acionamentos do botao.
        if (e.OriginalSource is DependencyObject origem && EstaSobreControle(origem))
            return;

        AlternarTelaCheia();
        e.Handled = true;
    }

    private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Encostar na janela ja silencia o alerta de fim: se o professor foi ate
        // o computador, ele evidentemente percebeu que o tempo acabou.
        _vm.SilenciarAlerta();

        // Arrastavel por qualquer ponto, exceto sobre controles interativos.
        if (e.OriginalSource is DependencyObject origem && EstaSobreControle(origem))
            return;

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove lanca se o botao ja foi solto; ignorar e seguro.
        }
    }

    private static bool EstaSobreControle(DependencyObject origem)
    {
        var atual = origem;
        while (atual is not null)
        {
            if (atual is Button or TextBox or MenuItem)
                return true;
            atual = VisualTreeHelper.GetParent(atual);
        }
        return false;
    }

    private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Ctrl + roda ajusta a transparencia entre 30% e 100%.
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            return;

        var passo = e.Delta > 0 ? 0.05 : -0.05;
        _settings.Opacity = Math.Clamp(_settings.Opacity + passo, 0.30, 1.00);

        // Mostra o efeito na hora, mesmo com o mouse sobre a janela.
        Opacity = _settings.Opacity;
        _mouseSobreJanela = false;

        e.Handled = true;
    }

    // ------------------------------------------------------------------
    // Botoes
    // ------------------------------------------------------------------

    private void BotaoPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int minutos })
            _vm.PresetClicked(minutos);
    }

    private void BotaoPrimario_Click(object sender, RoutedEventArgs e) => _vm.ToggleStartPause();

    private void BotaoZerar_Click(object sender, RoutedEventArgs e) => _vm.Reset();

    private void BotaoMaisUm_Click(object sender, RoutedEventArgs e) => _vm.AddMinutes(1);

    private void BotaoCarregar_Click(object sender, RoutedEventArgs e) => CarregarTempoDigitado();

    private void CampoTempo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CarregarTempoDigitado();
            e.Handled = true;
        }
    }

    private void CarregarTempoDigitado()
    {
        if (TimeParser.TryParse(CampoTempo.Text, out var tempo) && tempo > TimeSpan.Zero)
        {
            _vm.Load(tempo);
            CampoTempo.Clear();
            Keyboard.ClearFocus();
        }
        else
        {
            MessageBox.Show(
                "Não entendi esse tempo.\n\nEscreva no formato MM:SS, como 25:30. "
                + "Para mais de uma hora, use HH:MM:SS, como 01:05:00. "
                + "Se preferir, digite só os minutos, como 25.",
                "CronoAula",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    // ------------------------------------------------------------------
    // Menu de contexto
    // ------------------------------------------------------------------

    private void MenuTamanho_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string tag } && Enum.TryParse<DisplaySize>(tag, out var tamanho))
            AplicarTamanho(tamanho);
    }

    private void MenuOpacidade_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string tag }
            && double.TryParse(tag, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out var valor))
        {
            _settings.Opacity = Math.Clamp(valor, 0.30, 1.00);
            _mouseSobreJanela = false;
            AplicarOpacidade();
        }
    }

    private void MenuCanto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string canto })
            MoverParaCanto(canto);
    }

    private void MenuSempreTopo_Click(object sender, RoutedEventArgs e)
    {
        _settings.AlwaysOnTop = MenuSempreTopo.IsChecked;
        Topmost = _settings.AlwaysOnTop;

        if (_settings.AlwaysOnTop)
        {
            _reforcoTopo.Start();
            ReafirmarTopo();
        }
        else
        {
            _reforcoTopo.Stop();
        }
    }

    private void MenuBarraTarefas_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowInTaskbar = MenuBarraTarefas.IsChecked;
        ShowInTaskbar = _settings.ShowInTaskbar;
    }

    private void MenuPreferencias_Click(object sender, RoutedEventArgs e)
    {
        var janela = new PreferencesWindow(_settings) { Owner = this };

        if (janela.ShowDialog() != true)
            return;

        // Reaplica tudo que pode ter mudado.
        _vm.ApplySettings();
        ConstruirPresets();
        AplicarOpacidade();
        RegistrarAtalhos(avisar: true);
        AtualizarMostrador();
        _settings.Save();
    }

    private void MenuSobre_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private void MenuSilenciar_Click(object sender, RoutedEventArgs e) => _vm.SilenciarAlerta();

    private void MenuTelaCheia_Click(object sender, RoutedEventArgs e)
    {
        // Com varios monitores este item vira submenu e nao dispara sozinho;
        // quem age sao os itens filhos. Com um monitor so, ele proprio alterna.
        if (MenuTelaCheia.Items.Count == 0)
            AlternarTelaCheia();
    }


    private void MenuSair_Click(object sender, RoutedEventArgs e) => Close();

    // ------------------------------------------------------------------
    // Atalhos globais
    // ------------------------------------------------------------------

    private void Hotkeys_Pressed(object? sender, HotkeyAction acao)
    {
        switch (acao)
        {
            case HotkeyAction.IniciarPausar:
                _vm.ToggleStartPause();
                break;
            case HotkeyAction.Zerar:
                _vm.Reset();
                break;
            case HotkeyAction.AdicionarMinuto:
                _vm.AddMinutes(1);
                break;
            case HotkeyAction.SubtrairMinuto:
                _vm.AddMinutes(-1);
                break;
            case HotkeyAction.MostrarEsconder:
                AlternarVisibilidade();
                break;
            case HotkeyAction.TelaCheia:
                // Util durante a prova: o foco costuma estar em outro programa.
                AlternarTelaCheia();
                break;
        }
    }

    private void AlternarVisibilidade()
    {
        if (IsVisible)
        {
            // Guarda a posicao antes de esconder, para restaurar no mesmo lugar.
            _settings.Left = Left;
            _settings.Top = Top;
            Hide();
        }
        else
        {
            Show();
            RestaurarPosicao();
            ReafirmarTopo();
        }
    }
}

