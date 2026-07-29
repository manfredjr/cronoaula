using System.Diagnostics;
using CronoAula.Core;

namespace CronoAula.Tests;

/// <summary>
/// Relogio falso controlado manualmente: permite testar o TimerEngine sem esperar
/// o tempo real passar. Simula o comportamento acumulativo de um Stopwatch.
/// </summary>
internal sealed class FakeClock : IClock
{
    private TimeSpan _accumulated = TimeSpan.Zero;
    private TimeSpan _startedAt = TimeSpan.Zero;
    public TimeSpan Now { get; set; } = TimeSpan.Zero;

    public bool IsRunning { get; private set; }

    public TimeSpan Elapsed => IsRunning ? _accumulated + (Now - _startedAt) : _accumulated;

    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        _startedAt = Now;
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _accumulated += Now - _startedAt;
        IsRunning = false;
    }

    public void Reset()
    {
        _accumulated = TimeSpan.Zero;
        _startedAt = Now;
        IsRunning = false;
    }

    /// <summary>Avanca o "relogio" em delta.</summary>
    public void Advance(TimeSpan delta) => Now += delta;
}

public class TimerEngineTests
{
    [Fact]
    public void Load_DeixaProntoComTempoCheio_SemContar()
    {
        var clock = new FakeClock();
        var engine = new TimerEngine(clock);

        engine.Load(TimeSpan.FromMinutes(10));

        Assert.Equal(TimerState.Stopped, engine.State);
        Assert.Equal(TimeSpan.FromMinutes(10), engine.Remaining);

        clock.Advance(TimeSpan.FromMinutes(5));
        Assert.Equal(TimeSpan.FromMinutes(10), engine.Remaining); // parado nao consome tempo
    }

    [Fact]
    public void Start_ContaRegressivo()
    {
        var clock = new FakeClock();
        var engine = new TimerEngine(clock);
        engine.Load(TimeSpan.FromMinutes(10));

        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(3));

        Assert.Equal(TimerState.Running, engine.State);
        Assert.Equal(TimeSpan.FromMinutes(7), engine.Remaining);
    }

    [Fact]
    public void Pause_CongelaOTempo_ResumeContinua()
    {
        var clock = new FakeClock();
        var engine = new TimerEngine(clock);
        engine.Load(TimeSpan.FromMinutes(10));

        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(2));
        engine.Pause();

        clock.Advance(TimeSpan.FromMinutes(5)); // tempo passa, mas esta pausado
        Assert.Equal(TimeSpan.FromMinutes(8), engine.Remaining);
        Assert.Equal(TimerState.Paused, engine.State);

        engine.Resume();
        clock.Advance(TimeSpan.FromMinutes(3));
        Assert.Equal(TimeSpan.FromMinutes(5), engine.Remaining);
    }

    [Fact]
    public void Reset_VoltaAoTempoCheio()
    {
        var clock = new FakeClock();
        var engine = new TimerEngine(clock);
        engine.Load(TimeSpan.FromMinutes(10));
        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(4));

        engine.Reset();

        Assert.Equal(TimerState.Stopped, engine.State);
        Assert.Equal(TimeSpan.FromMinutes(10), engine.Remaining);
    }

    [Fact]
    public void AddTime_EstendeSemPerderContagem()
    {
        var clock = new FakeClock();
        var engine = new TimerEngine(clock);
        engine.Load(TimeSpan.FromMinutes(10));
        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(4)); // restante 6

        engine.AddTime(TimeSpan.FromMinutes(1)); // +1 min -> restante 7

        Assert.Equal(TimeSpan.FromMinutes(7), engine.Remaining);
    }

    [Fact]
    public void Overtime_ContaNegativoQuandoPermitido()
    {
        var clock = new FakeClock();
        var engine = new TimerEngine(clock) { AllowOvertime = true };
        engine.Load(TimeSpan.FromMinutes(1));
        engine.Start();

        clock.Advance(TimeSpan.FromSeconds(80)); // 20s alem do fim
        engine.Update();

        Assert.True(engine.IsOvertime);
        Assert.Equal(TimeSpan.FromSeconds(-20), engine.Remaining);
        Assert.Equal(TimerState.Running, engine.State); // continua rodando no negativo
    }

    [Fact]
    public void Overtime_Desligado_ParaEmZero()
    {
        var clock = new FakeClock();
        var engine = new TimerEngine(clock) { AllowOvertime = false };
        engine.Load(TimeSpan.FromMinutes(1));
        engine.Start();

        clock.Advance(TimeSpan.FromSeconds(80));
        engine.Update();

        Assert.False(engine.IsOvertime);
        Assert.Equal(TimeSpan.Zero, engine.Remaining);
        Assert.Equal(TimerState.Stopped, engine.State);
    }

    [Fact]
    public void Finished_DisparaUmaVezAoCruzarZero()
    {
        var clock = new FakeClock();
        var engine = new TimerEngine(clock);
        engine.Load(TimeSpan.FromSeconds(30));
        engine.Start();

        var count = 0;
        engine.Finished += (_, _) => count++;

        clock.Advance(TimeSpan.FromSeconds(29));
        engine.Update();
        Assert.Equal(0, count);

        clock.Advance(TimeSpan.FromSeconds(2)); // cruza o zero
        engine.Update();
        engine.Update(); // updates repetidos nao redisparam
        Assert.Equal(1, count);
    }

    [Fact]
    public void EarlyWarning_DisparaNoLimiar()
    {
        var clock = new FakeClock();
        var engine = new TimerEngine(clock)
        {
            EarlyWarningAt = TimeSpan.FromMinutes(2)
        };
        engine.Load(TimeSpan.FromMinutes(10));
        engine.Start();

        var raised = 0;
        engine.EarlyWarning += (_, _) => raised++;

        clock.Advance(TimeSpan.FromMinutes(7)); // restante 3 -> ainda nao
        engine.Update();
        Assert.Equal(0, raised);

        clock.Advance(TimeSpan.FromMinutes(1.5)); // restante 1.5 -> cruzou 2 min
        engine.Update();
        engine.Update();
        Assert.Equal(1, raised);
    }

    [Fact]
    public void AddTime_ReabilitaFinished_AposEstender()
    {
        var clock = new FakeClock();
        var engine = new TimerEngine(clock);
        engine.Load(TimeSpan.FromSeconds(10));
        engine.Start();

        var count = 0;
        engine.Finished += (_, _) => count++;

        clock.Advance(TimeSpan.FromSeconds(11));
        engine.Update();
        Assert.Equal(1, count);

        engine.AddTime(TimeSpan.FromSeconds(30)); // estende: restante volta a ~29s
        clock.Advance(TimeSpan.FromSeconds(30));  // cruza o zero de novo
        engine.Update();
        Assert.Equal(2, count);
    }

    /// <summary>
    /// Garante o requisito central de precisao: o restante e sempre calculado a
    /// partir do relogio, e nao de uma soma de ticks. Milhares de chamadas a
    /// Update() nao introduzem desvio algum.
    /// </summary>
    [Fact]
    public void SemDeriva_MuitosUpdatesNaoAcumulamErro()
    {
        var clock = new FakeClock();
        var engine = new TimerEngine(clock);
        engine.Load(TimeSpan.FromMinutes(10));
        engine.Start();

        // Simula 10 min avancando de 100 em 100 ms com um Update a cada passo.
        for (var i = 0; i < 6000; i++)
        {
            clock.Advance(TimeSpan.FromMilliseconds(100));
            engine.Update();
        }

        // Restante deve ser exatamente zero (600 s - 600 s), sem acumulo de erro.
        Assert.Equal(TimeSpan.Zero, engine.Remaining);
    }

    /// <summary>
    /// Prova de precisao contra o relogio real do sistema (proxy rapido do criterio
    /// de aceite de 10 min). Usando Stopwatch (QueryPerformanceCounter), o desvio em
    /// relacao a DateTime deve ser desprezivel; extrapola linearmente para 10 min.
    /// </summary>
    [Fact]
    public void PrecisaoReal_SemDesvioSignificativo()
    {
        var engine = new TimerEngine(new SystemClock());
        engine.Load(TimeSpan.FromMinutes(10));

        var wallStart = DateTime.UtcNow;
        engine.Start();

        Thread.Sleep(1500); // 1,5 s reais
        var wallElapsed = DateTime.UtcNow - wallStart;

        var consumed = TimeSpan.FromMinutes(10) - engine.Remaining; // quanto o engine "gastou"
        var drift = (consumed - wallElapsed).Duration();

        // Toleramos 50 ms em 1,5 s. Isso corresponde a << 1 s em 10 min.
        Assert.True(drift < TimeSpan.FromMilliseconds(50), $"Desvio de {drift.TotalMilliseconds:F1} ms excedeu 50 ms");
    }
}
