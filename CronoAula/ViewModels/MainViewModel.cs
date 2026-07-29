using System.ComponentModel;
using System.Runtime.CompilerServices;
using CronoAula.Core;

namespace CronoAula.ViewModels;

/// <summary>Faixas de alerta visual, do tempo confortavel ao tempo estourado.</summary>
public enum AlertLevel
{
    /// <summary>Tempo normal: numeros claros sobre fundo escuro.</summary>
    Normal,
    /// <summary>Ultimos 20% do tempo (ou 2 minutos, o que for menor): amarelo.</summary>
    Atencao,
    /// <summary>Ultimo minuto: laranja.</summary>
    Urgente,
    /// <summary>Zero ou tempo excedido: vermelho piscando suavemente.</summary>
    Estourado
}

/// <summary>
/// MVVM leve: faz a ponte entre o <see cref="TimerEngine"/> (logica pura, testavel)
/// e a janela. Nao contem nenhuma chamada ao Win32 nem manipulacao de controles;
/// isso fica no code-behind da janela.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly TimerEngine _engine;
    private readonly SoundService _sound;
    private readonly AppSettings _settings;

    public MainViewModel(AppSettings settings, SoundService sound, TimerEngine? engine = null)
    {
        _settings = settings;
        _sound = sound;
        _engine = engine ?? new TimerEngine();

        _engine.AllowOvertime = settings.AllowOvertime;
        _engine.Finished += OnFinished;
        _engine.EarlyWarning += OnEarlyWarning;
        _engine.StateChanged += (_, _) => RaiseStateProperties();

        LoadMinutes(settings.LastMinutes);
    }

    public TimerEngine Engine => _engine;

    // ------------------------------------------------------------------
    // Propriedades observadas pela interface
    // ------------------------------------------------------------------

    private string _display = "00:00";
    /// <summary>Texto do cronometro, ja formatado (MM:SS, HH:MM:SS ou -MM:SS).</summary>
    public string Display
    {
        get => _display;
        private set => Set(ref _display, value);
    }

    private AlertLevel _alert = AlertLevel.Normal;
    public AlertLevel Alert
    {
        get => _alert;
        private set => Set(ref _alert, value);
    }

    /// <summary>Rotulo do botao principal, que alterna conforme o estado.</summary>
    public string PrimaryButtonLabel => _engine.State switch
    {
        TimerState.Running => "Pausar",
        TimerState.Paused => "Continuar",
        _ => "Iniciar"
    };

    public bool IsRunning => _engine.State == TimerState.Running;

    /// <summary>Preset atualmente "armado" (carregado, aguardando o segundo clique).</summary>
    private int? _armedPreset;
    public int? ArmedPreset
    {
        get => _armedPreset;
        private set => Set(ref _armedPreset, value);
    }

    // ------------------------------------------------------------------
    // Acoes
    // ------------------------------------------------------------------

    /// <summary>
    /// Comportamento dos botoes de tempo rapido, documentado no README:
    /// o primeiro clique carrega o tempo (deixa o botao destacado) e o segundo
    /// clique no mesmo botao inicia a contagem. Isso evita partidas acidentais.
    /// </summary>
    public void PresetClicked(int minutes)
    {
        if (ArmedPreset == minutes && _engine.State != TimerState.Running)
        {
            Start();
            return;
        }

        LoadMinutes(minutes);
        ArmedPreset = minutes;
    }

    public void LoadMinutes(double minutes)
    {
        Load(TimeSpan.FromMinutes(minutes));
    }

    public void Load(TimeSpan duration)
    {
        _sound.StopAlert();

        _engine.AllowOvertime = _settings.AllowOvertime;
        _engine.EarlyWarningAt = _settings.EarlyWarningEnabled
            ? TimeSpan.FromMinutes(_settings.EarlyWarningMinutes)
            : null;

        _engine.Load(duration);
        _settings.LastMinutes = duration.TotalMinutes;
        ArmedPreset = null;
        Refresh();
    }

    public void Start()
    {
        _sound.StopAlert();
        _engine.Start();
        ArmedPreset = null;
        Refresh();
    }

    public void Pause()
    {
        _sound.StopAlert();
        _engine.Pause();
        Refresh();
    }

    /// <summary>Alterna entre iniciar/continuar e pausar (botao principal e atalho).</summary>
    public void ToggleStartPause()
    {
        if (_engine.State == TimerState.Running)
            Pause();
        else
            Start();
    }

    public void Reset()
    {
        _sound.StopAlert();
        _engine.Reset();
        ArmedPreset = null;
        Refresh();
    }

    public void AddMinutes(int minutes)
    {
        _sound.StopAlert();
        _engine.AddTime(TimeSpan.FromMinutes(minutes));
        _settings.LastMinutes = _engine.Duration.TotalMinutes;
        Refresh();
    }

    /// <summary>
    /// Chamado periodicamente pela janela enquanto conta. Repassa ao engine para
    /// que os limiares sejam avaliados e atualiza o texto exibido.
    /// </summary>
    public void Tick()
    {
        _engine.Update();
        Refresh();
    }

    /// <summary>Reaplica preferencias alteradas na janela de configuracoes.</summary>
    public void ApplySettings()
    {
        _engine.AllowOvertime = _settings.AllowOvertime;
        _engine.EarlyWarningAt = _settings.EarlyWarningEnabled
            ? TimeSpan.FromMinutes(_settings.EarlyWarningMinutes)
            : null;
        Refresh();
    }

    // ------------------------------------------------------------------
    // Interno
    // ------------------------------------------------------------------

    private void Refresh()
    {
        var remaining = _engine.Remaining;
        Display = TimeParser.Format(remaining);
        Alert = ComputeAlert(remaining, _engine.Duration);
        RaiseStateProperties();
    }

    /// <summary>
    /// Regras de cor: amarelo nos ultimos 20% do tempo ou nos ultimos 2 minutos,
    /// o que for menor; laranja no ultimo minuto; vermelho ao zerar ou estourar.
    /// </summary>
    internal static AlertLevel ComputeAlert(TimeSpan remaining, TimeSpan duration)
    {
        if (remaining <= TimeSpan.Zero)
            return AlertLevel.Estourado;

        if (remaining <= TimeSpan.FromMinutes(1))
            return AlertLevel.Urgente;

        var twentyPercent = TimeSpan.FromTicks((long)(duration.Ticks * 0.20));
        var threshold = twentyPercent < TimeSpan.FromMinutes(2)
            ? twentyPercent
            : TimeSpan.FromMinutes(2);

        return remaining <= threshold ? AlertLevel.Atencao : AlertLevel.Normal;
    }

    private void OnFinished(object? sender, EventArgs e)
    {
        // Sequencia repetida: o professor pode estar longe do computador ou
        // atendendo um aluno quando o tempo acaba.
        _sound.StartAlert(
            _settings.SoundEnabled,
            _settings.Volume,
            _settings.AlertRepetitions,
            _settings.AlertIntervalSeconds);

        Finished?.Invoke(this, EventArgs.Empty);
    }

    private void OnEarlyWarning(object? sender, EventArgs e)
    {
        // Som proprio, discreto e tocado uma unica vez: nao pode ser confundido
        // com o alerta de fim.
        _sound.PlayWarning(_settings.SoundEnabled, _settings.Volume);
        EarlyWarning?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Interrompe o alerta sonoro do fim. Chamado sempre que o professor age
    /// sobre o cronometro: se ele ja percebeu, nao ha por que seguir chamando.
    /// </summary>
    public void SilenciarAlerta() => _sound.StopAlert();

    /// <summary>True enquanto a sequencia de alerta do fim ainda esta tocando.</summary>
    public bool AlertaSoando => _sound.AlertaAtivo;

    /// <summary>Notifica a janela para que ela possa reagir (piscar, por exemplo).</summary>
    public event EventHandler? Finished;
    public event EventHandler? EarlyWarning;

    private void RaiseStateProperties()
    {
        OnPropertyChanged(nameof(PrimaryButtonLabel));
        OnPropertyChanged(nameof(IsRunning));
    }

    // --- INotifyPropertyChanged ---

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        OnPropertyChanged(name);
    }
}
