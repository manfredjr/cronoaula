using System.IO;
using System.Media;
using System.Reflection;

namespace CronoAula.Core;

/// <summary>
/// Toca os sinais sonoros do cronometro.
///
/// Sao dois sons deliberadamente diferentes:
///  - aviso.wav  : aviso antecipado. Um toque unico e discreto.
///  - alerta.wav : fim do tempo. Padrao de duas notas alternadas, repetido
///                 varias vezes com pausa entre as repeticoes, para o professor
///                 perceber mesmo longe do computador ou atendendo um aluno.
///
/// Os dois arquivos sao embutidos no assembly (EmbeddedResource), e nao lidos do
/// disco. E isso que permite gerar um executavel unico: nao existe arquivo solto
/// ao lado do .exe para se perder.
///
/// Sobre o volume: System.Media.SoundPlayer nao expoe controle de volume. Em vez
/// de trazer uma dependencia externa so por causa disso, ajustamos a amplitude
/// das proprias amostras PCM em memoria antes de entregar o fluxo ao SoundPlayer.
/// </summary>
public sealed class SoundService : IDisposable
{
    private const string RecursoAlerta = "CronoAula.Assets.alerta.wav";
    private const string RecursoAviso = "CronoAula.Assets.aviso.wav";

    /// <summary>
    /// Teto de seguranca para a repeticao "ate parar". Sem ele, o cronometro
    /// deixado aberto numa sala vazia tocaria indefinidamente.
    /// </summary>
    internal const int MaxRepeticoes = 60;

    private readonly SomEmCache _alerta = new(RecursoAlerta);
    private readonly SomEmCache _aviso = new(RecursoAviso);

    private readonly object _trava = new();
    private System.Timers.Timer? _timerRepeticao;
    private int _repeticoesRestantes;
    private double _volumeAtual;
    private bool _descartado;

    /// <summary>True enquanto a sequencia de alerta do fim ainda esta tocando.</summary>
    public bool AlertaAtivo
    {
        get { lock (_trava) { return _timerRepeticao is not null; } }
    }

    /// <summary>
    /// Aviso antecipado: toca uma unica vez, discreto.
    /// </summary>
    public void PlayWarning(bool habilitado, double volume)
    {
        if (!habilitado || volume <= 0)
            return;

        // Um pouco mais baixo que o alerta do fim, para nao ser confundido com ele.
        _aviso.Tocar(Math.Clamp(volume, 0.0, 1.0) * 0.7);
    }

    /// <summary>
    /// Fim do tempo: inicia a sequencia repetida.
    /// </summary>
    /// <param name="repeticoes">
    /// Quantas vezes tocar. Use 0 para repetir ate alguem interromper
    /// (limitado a <see cref="MaxRepeticoes"/> por seguranca).
    /// </param>
    /// <param name="intervaloSegundos">Tempo entre o inicio de uma repeticao e a seguinte.</param>
    public void StartAlert(bool habilitado, double volume, int repeticoes, double intervaloSegundos)
    {
        StopAlert();

        if (!habilitado || volume <= 0)
            return;

        volume = Math.Clamp(volume, 0.0, 1.0);
        intervaloSegundos = Math.Clamp(intervaloSegundos, 1.0, 30.0);

        var total = repeticoes <= 0
            ? MaxRepeticoes
            : Math.Min(repeticoes, MaxRepeticoes);

        lock (_trava)
        {
            if (_descartado)
                return;

            _volumeAtual = volume;
            _repeticoesRestantes = total;

            // Primeira repeticao sai na hora; o timer cuida das seguintes.
            TocarAlertaUmaVez();

            if (_repeticoesRestantes <= 0)
                return;

            _timerRepeticao = new System.Timers.Timer(intervaloSegundos * 1000)
            {
                AutoReset = true
            };
            _timerRepeticao.Elapsed += AoRepetir;
            _timerRepeticao.Start();
        }
    }

    /// <summary>
    /// Interrompe a sequencia de alerta. Chamado quando o professor mexe no
    /// cronometro (iniciar, pausar, zerar, carregar outro tempo): se ele ja
    /// percebeu que acabou, nao faz sentido continuar chamando a atencao.
    /// </summary>
    public void StopAlert()
    {
        System.Timers.Timer? aDescartar;

        lock (_trava)
        {
            aDescartar = _timerRepeticao;
            _timerRepeticao = null;
            _repeticoesRestantes = 0;
        }

        if (aDescartar is null)
            return;

        aDescartar.Elapsed -= AoRepetir;
        aDescartar.Stop();
        aDescartar.Dispose();
    }

    private void AoRepetir(object? sender, System.Timers.ElapsedEventArgs e)
    {
        bool acabou;

        lock (_trava)
        {
            if (_timerRepeticao is null || _descartado)
                return;

            TocarAlertaUmaVez();
            acabou = _repeticoesRestantes <= 0;
        }

        // Fora da trava: StopAlert tambem a utiliza.
        if (acabou)
            StopAlert();
    }

    /// <summary>Deve ser chamado com a trava tomada.</summary>
    private void TocarAlertaUmaVez()
    {
        if (_repeticoesRestantes <= 0)
            return;

        _repeticoesRestantes--;
        _alerta.Tocar(_volumeAtual);
    }

    public void Dispose()
    {
        lock (_trava)
        {
            _descartado = true;
        }

        StopAlert();
        _alerta.Dispose();
        _aviso.Dispose();
    }

    // ------------------------------------------------------------------
    // Ajuste de volume nas amostras PCM
    // ------------------------------------------------------------------

    /// <summary>
    /// Multiplica as amostras de 16 bits pelo fator de volume, preservando o
    /// cabecalho do arquivo. Localiza o bloco "data" percorrendo os chunks RIFF,
    /// em vez de assumir um cabecalho de tamanho fixo.
    /// </summary>
    internal static byte[] ApplyVolume(byte[] wav, double volume)
    {
        var result = (byte[])wav.Clone();

        if (volume >= 0.999)
            return result; // volume cheio: nada a fazer

        var dataOffset = FindDataChunk(result, out var dataLength);
        if (dataOffset < 0)
            return result; // formato inesperado: devolve intacto

        // Percorre pares de bytes (amostras PCM de 16 bits, little-endian).
        var end = Math.Min(dataOffset + dataLength, result.Length - 1);
        for (var i = dataOffset; i < end; i += 2)
        {
            var sample = BitConverter.ToInt16(result, i);
            var scaled = (int)Math.Round(sample * volume);
            scaled = Math.Clamp(scaled, short.MinValue, short.MaxValue);
            BitConverter.GetBytes((short)scaled).CopyTo(result, i);
        }

        return result;
    }

    /// <summary>
    /// Percorre a estrutura RIFF ate encontrar o chunk "data".
    /// Layout: "RIFF" tamanho "WAVE" e entao uma sequencia de chunks
    /// (identificador de 4 bytes + tamanho de 4 bytes + conteudo).
    /// </summary>
    private static int FindDataChunk(byte[] wav, out int length)
    {
        length = 0;

        if (wav.Length < 12)
            return -1;

        if (wav[0] != 'R' || wav[1] != 'I' || wav[2] != 'F' || wav[3] != 'F')
            return -1;

        var pos = 12; // pula "RIFF" + tamanho + "WAVE"
        while (pos + 8 <= wav.Length)
        {
            var id = System.Text.Encoding.ASCII.GetString(wav, pos, 4);
            var size = BitConverter.ToInt32(wav, pos + 4);
            if (size < 0)
                return -1;

            var contentStart = pos + 8;
            if (id == "data")
            {
                length = Math.Min(size, wav.Length - contentStart);
                return contentStart;
            }

            // Chunks tem tamanho par: um byte de preenchimento pode existir.
            pos = contentStart + size + (size % 2);
        }

        return -1;
    }

    /// <summary>
    /// Guarda um som embutido e o player ja preparado, reaproveitando-o
    /// enquanto o volume nao mudar. Evita reprocessar as amostras a cada toque.
    /// </summary>
    private sealed class SomEmCache : IDisposable
    {
        private readonly string _recurso;
        private byte[]? _original;
        private SoundPlayer? _player;
        private double _volumePreparado = -1;

        public SomEmCache(string recurso) => _recurso = recurso;

        public void Tocar(double volume)
        {
            try
            {
                if (_player is null || Math.Abs(_volumePreparado - volume) > 0.001)
                {
                    var wav = Carregar();
                    if (wav is null)
                    {
                        SystemSounds.Asterisk.Play(); // recurso ausente: som do sistema
                        return;
                    }

                    _player?.Dispose();
                    _player = new SoundPlayer(new MemoryStream(ApplyVolume(wav, volume)));
                    _player.Load();
                    _volumePreparado = volume;
                }

                _player.Play(); // assincrono: nao trava a interface
            }
            catch
            {
                // Maquina sem placa de som ou dispositivo ocupado nunca deve
                // derrubar o aplicativo.
                try { SystemSounds.Asterisk.Play(); } catch { /* sem audio */ }
            }
        }

        private byte[]? Carregar()
        {
            if (_original is not null)
                return _original;

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(_recurso);
            if (stream is null)
                return null;

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            _original = ms.ToArray();
            return _original;
        }

        public void Dispose()
        {
            _player?.Dispose();
            _player = null;
        }
    }
}
