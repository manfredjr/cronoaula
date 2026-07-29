namespace CronoAula.Core;

public enum TimerState
{
    /// <summary>Parado: nao esta contando (recem-carregado ou zerado).</summary>
    Stopped,
    /// <summary>Contando.</summary>
    Running,
    /// <summary>Pausado, com tempo decorrido preservado.</summary>
    Paused
}

/// <summary>
/// Logica pura do cronometro de contagem regressiva. NAO referencia WPF, o que a
/// torna testavel sem interface grafica.
///
/// A precisao vem sempre de um relogio real (<see cref="IClock"/> -> Stopwatch),
/// nunca da soma de ticks de um timer da UI. O <see cref="Remaining"/> e sempre
/// calculado como (duracao configurada - tempo decorrido no relogio), portanto
/// nao acumula erro em contagens longas.
///
/// A UI deve chamar <see cref="Update"/> periodicamente (ex.: a cada 100-250 ms)
/// para que os eventos de limiar (<see cref="EarlyWarning"/> e <see cref="Finished"/>)
/// sejam disparados nos momentos certos. A leitura de <see cref="Remaining"/> em si
/// nao tem efeitos colaterais.
/// </summary>
public sealed class TimerEngine
{
    private readonly IClock _clock;
    private TimeSpan _duration;      // duracao alvo configurada (inclui extensoes de +1 min)
    private bool _finishedRaised;    // garante que Finished dispare so uma vez por cruzamento
    private bool _earlyRaised;       // idem para o aviso antecipado

    public TimerEngine(IClock? clock = null)
    {
        _clock = clock ?? new SystemClock();
    }

    public TimerState State { get; private set; } = TimerState.Stopped;

    /// <summary>
    /// Quando true (padrao), ao chegar a zero o cronometro continua contando em
    /// negativo (tempo excedido). Quando false, ele para exatamente em 00:00.
    /// </summary>
    public bool AllowOvertime { get; set; } = true;

    /// <summary>
    /// Se definido, dispara <see cref="EarlyWarning"/> quando o tempo restante
    /// cruzar esse limiar (ex.: faltando 05:00). Null desliga o aviso antecipado.
    /// </summary>
    public TimeSpan? EarlyWarningAt { get; set; }

    /// <summary>Disparado uma vez quando a contagem cruza o zero.</summary>
    public event EventHandler? Finished;

    /// <summary>Disparado uma vez quando o restante cruza <see cref="EarlyWarningAt"/>.</summary>
    public event EventHandler? EarlyWarning;

    /// <summary>Disparado sempre que o estado (Stopped/Running/Paused) muda.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Duracao alvo atualmente configurada.</summary>
    public TimeSpan Duration => _duration;

    /// <summary>
    /// Tempo restante. Pode ser negativo quando <see cref="AllowOvertime"/> e true
    /// e a atividade estourou. Se AllowOvertime for false, nunca fica abaixo de zero.
    /// </summary>
    public TimeSpan Remaining
    {
        get
        {
            var rem = _duration - _clock.Elapsed;
            if (!AllowOvertime && rem < TimeSpan.Zero)
                return TimeSpan.Zero;
            return rem;
        }
    }

    /// <summary>True quando a atividade estourou o tempo (restante negativo).</summary>
    public bool IsOvertime => Remaining < TimeSpan.Zero;

    /// <summary>
    /// Carrega um tempo sem iniciar a contagem. Deixa o cronometro pronto (Stopped)
    /// exibindo a duracao cheia.
    /// </summary>
    public void Load(TimeSpan duration)
    {
        _duration = Clamp(duration);
        _clock.Reset();
        _finishedRaised = false;
        _earlyRaised = false;
        SetState(TimerState.Stopped);
    }

    /// <summary>Inicia ou retoma a contagem.</summary>
    public void Start()
    {
        if (State == TimerState.Running)
            return;
        _clock.Start();
        SetState(TimerState.Running);
    }

    /// <summary>Pausa, preservando o tempo decorrido.</summary>
    public void Pause()
    {
        if (State != TimerState.Running)
            return;
        _clock.Stop();
        SetState(TimerState.Paused);
    }

    /// <summary>Retoma a contagem apos uma pausa (equivalente a <see cref="Start"/>).</summary>
    public void Resume() => Start();

    /// <summary>
    /// Zera a contagem, voltando ao tempo cheio configurado e ao estado Stopped.
    /// </summary>
    public void Reset()
    {
        _clock.Reset();
        _finishedRaised = false;
        _earlyRaised = false;
        SetState(TimerState.Stopped);
    }

    /// <summary>
    /// Estende (ou reduz, com delta negativo) a atividade em andamento sem perder a
    /// contagem ja decorrida. Usado pelo "+1 min" e "-1 min".
    /// </summary>
    public void AddTime(TimeSpan delta)
    {
        _duration = Clamp(_duration + delta);
        // Se a extensao trouxe o restante de volta para o positivo, permite que
        // Finished dispare de novo quando cruzar o zero mais tarde.
        if (_duration - _clock.Elapsed > TimeSpan.Zero)
            _finishedRaised = false;
    }

    /// <summary>
    /// Deve ser chamado periodicamente pela UI enquanto conta. Detecta cruzamentos
    /// de limiar e dispara <see cref="EarlyWarning"/> e <see cref="Finished"/>.
    /// A leitura de <see cref="Remaining"/> permanece sem efeitos colaterais.
    /// </summary>
    public void Update()
    {
        if (State != TimerState.Running)
            return;

        var rem = _duration - _clock.Elapsed;

        if (EarlyWarningAt.HasValue && !_earlyRaised
            && rem <= EarlyWarningAt.Value && rem > TimeSpan.Zero)
        {
            _earlyRaised = true;
            EarlyWarning?.Invoke(this, EventArgs.Empty);
        }

        if (rem <= TimeSpan.Zero && !_finishedRaised)
        {
            _finishedRaised = true;
            Finished?.Invoke(this, EventArgs.Empty);

            if (!AllowOvertime)
            {
                _clock.Stop();
                SetState(TimerState.Stopped);
            }
        }
    }

    private void SetState(TimerState state)
    {
        if (State == state)
            return;
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static TimeSpan Clamp(TimeSpan value) =>
        value < TimeSpan.Zero ? TimeSpan.Zero : value;
}
