using System.Diagnostics;

namespace CronoAula.Core;

/// <summary>
/// Fonte de tempo do cronometro. Abstraida para que o <see cref="TimerEngine"/>
/// possa ser testado sem depender do tempo real (nos testes injetamos um relogio falso).
/// </summary>
public interface IClock
{
    /// <summary>Tempo decorrido acumulado enquanto o relogio esteve rodando.</summary>
    TimeSpan Elapsed { get; }

    bool IsRunning { get; }

    /// <summary>Inicia/retoma a contagem. Acumula sobre o tempo anterior (como um Stopwatch).</summary>
    void Start();

    /// <summary>Pausa a contagem, preservando o tempo ja decorrido.</summary>
    void Stop();

    /// <summary>Zera o tempo decorrido.</summary>
    void Reset();
}

/// <summary>
/// Implementacao real baseada em <see cref="Stopwatch"/>, que usa o contador de
/// alta precisao do sistema (QueryPerformanceCounter). E o relogio "de verdade"
/// exigido: a contagem nunca deriva por somar ticks de um DispatcherTimer.
/// </summary>
public sealed class SystemClock : IClock
{
    private readonly Stopwatch _sw = new();

    public TimeSpan Elapsed => _sw.Elapsed;
    public bool IsRunning => _sw.IsRunning;

    public void Start() => _sw.Start();
    public void Stop() => _sw.Stop();

    public void Reset() => _sw.Reset();
}
