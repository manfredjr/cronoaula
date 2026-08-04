using System.Threading;

namespace CronoAula.Core;

/// <summary>
/// Garante que apenas uma copia do CronoAula rode por vez.
///
/// Por que isso importa: os atalhos globais sao exclusivos no Windows. Quem
/// registra primeiro fica com eles. Se o professor esconde a janela com
/// Ctrl+Alt+H, esquece, e abre o programa de novo, a segunda copia nao consegue
/// registrar atalho nenhum, e as teclas continuam indo para a copia escondida.
/// O sintoma e desconcertante: os atalhos "param de funcionar" sem motivo
/// aparente.
///
/// Com esta trava, abrir o programa uma segunda vez apenas traz de volta a
/// janela que ja estava rodando.
///
/// Sobre a comunicacao entre as copias: usamos um evento nomeado do sistema.
/// A primeira versao deste codigo enviava PostMessage para HWND_BROADCAST, o
/// que funcionava, mas fazia o Smart App Control bloquear o executavel:
/// disparar mensagens para todas as janelas do sistema e um padrao usado por
/// programas maliciosos. Um evento nomeado resolve o mesmo problema sem tocar
/// em janela alguma.
/// </summary>
public sealed class InstanciaUnica : IDisposable
{
    // Nomes fixos e por sessao (Local), para nao conflitar entre contas.
    private const string NomeMutex = @"Local\CronoAula.InstanciaUnica";
    private const string NomeEvento = @"Local\CronoAula.MostrarJanela";

    private Mutex? _mutex;
    private EventWaitHandle? _evento;
    private Thread? _vigia;
    private CancellationTokenSource? _parar;
    private bool _descartado;

    /// <summary>
    /// Disparado, na thread de vigilancia, quando outra copia do programa pede
    /// que esta mostre a janela. Quem assina precisa voltar para a thread da
    /// interface antes de mexer em controles.
    /// </summary>
    public event EventHandler? PedidoDeMostrarJanela;

    /// <summary>
    /// Tenta assumir a posicao de unica instancia.
    /// Devolve true se esta copia e a primeira; false se ja existe outra rodando.
    /// </summary>
    public bool TentarAssumir()
    {
        _mutex = new Mutex(initiallyOwned: true, NomeMutex, out var criouAgora);

        if (!criouAgora)
        {
            _mutex.Dispose();
            _mutex = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Passa a escutar pedidos vindos de outras copias do programa.
    /// Deve ser chamado apenas pela instancia que assumiu.
    /// </summary>
    public void EscutarPedidos()
    {
        _evento = new EventWaitHandle(false, EventResetMode.AutoReset, NomeEvento);
        _parar = new CancellationTokenSource();

        var evento = _evento;
        var token = _parar.Token;

        _vigia = new Thread(() =>
        {
            var alcas = new WaitHandle[] { evento, token.WaitHandle };

            while (!token.IsCancellationRequested)
            {
                // Espera bloqueante: nao consome CPU enquanto nada acontece.
                var qual = WaitHandle.WaitAny(alcas);

                if (qual != 0 || token.IsCancellationRequested)
                    return;

                PedidoDeMostrarJanela?.Invoke(this, EventArgs.Empty);
            }
        })
        {
            IsBackground = true,
            Name = "CronoAula.VigiaDeInstancia"
        };

        _vigia.Start();
    }

    /// <summary>
    /// Pede a copia que ja esta rodando que mostre a janela dela.
    /// Chamado pela segunda copia, logo antes de encerrar.
    /// </summary>
    public static void PedirParaMostrarJanelaExistente()
    {
        try
        {
            if (EventWaitHandle.TryOpenExisting(NomeEvento, out var evento))
            {
                using (evento)
                    evento.Set();
            }
        }
        catch
        {
            // Se nao der para avisar, a segunda copia simplesmente encerra.
            // O professor vera a janela original onde ela ja estava.
        }
    }

    public void Dispose()
    {
        if (_descartado)
            return;
        _descartado = true;

        _parar?.Cancel();
        _vigia?.Join(TimeSpan.FromSeconds(1));
        _parar?.Dispose();
        _evento?.Dispose();

        try
        {
            _mutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // O mutex nao era desta thread; nada a fazer.
        }

        _mutex?.Dispose();
        _mutex = null;
    }
}
