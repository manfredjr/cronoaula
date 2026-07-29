using CronoAula.Core;

namespace CronoAula.Tests;

/// <summary>
/// O volume e aplicado escalando as amostras PCM em memoria, ja que o
/// System.Media.SoundPlayer nao tem controle de volume proprio.
/// </summary>
public class SoundServiceTests
{
    /// <summary>Monta um WAV minimo (PCM 16 bits, mono) com as amostras dadas.</summary>
    private static byte[] MontarWav(params short[] amostras)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        var dados = amostras.Length * 2;

        bw.Write("RIFF".ToCharArray());
        bw.Write(36 + dados);
        bw.Write("WAVE".ToCharArray());
        bw.Write("fmt ".ToCharArray());
        bw.Write(16);              // tamanho do bloco fmt
        bw.Write((short)1);        // PCM
        bw.Write((short)1);        // mono
        bw.Write(44100);           // taxa de amostragem
        bw.Write(44100 * 2);       // bytes por segundo
        bw.Write((short)2);        // alinhamento
        bw.Write((short)16);       // bits por amostra
        bw.Write("data".ToCharArray());
        bw.Write(dados);
        foreach (var a in amostras)
            bw.Write(a);

        bw.Flush();
        return ms.ToArray();
    }

    private static short[] LerAmostras(byte[] wav)
    {
        // O cabecalho canonico deste WAV tem 44 bytes.
        var total = (wav.Length - 44) / 2;
        var saida = new short[total];
        for (var i = 0; i < total; i++)
            saida[i] = BitConverter.ToInt16(wav, 44 + i * 2);
        return saida;
    }

    [Fact]
    public void MeioVolume_ReduzAmplitudePelaMetade()
    {
        var wav = MontarWav(1000, -1000, 20000, -20000);

        var saida = LerAmostras(SoundService.ApplyVolume(wav, 0.5));

        Assert.Equal(new short[] { 500, -500, 10000, -10000 }, saida);
    }

    [Fact]
    public void VolumeCheio_NaoAlteraAmostras()
    {
        var wav = MontarWav(1234, -4321, 32767, -32768);

        var saida = LerAmostras(SoundService.ApplyVolume(wav, 1.0));

        Assert.Equal(new short[] { 1234, -4321, 32767, -32768 }, saida);
    }

    [Fact]
    public void VolumeZero_Silencia()
    {
        var wav = MontarWav(15000, -15000, 300);

        var saida = LerAmostras(SoundService.ApplyVolume(wav, 0.0));

        Assert.All(saida, a => Assert.Equal(0, a));
    }

    [Fact]
    public void NaoEstoura_NosExtremosDoInt16()
    {
        // Mesmo no limite, o resultado precisa caber em short sem dar a volta.
        var wav = MontarWav(short.MinValue, short.MaxValue);

        var saida = LerAmostras(SoundService.ApplyVolume(wav, 0.999));

        Assert.All(saida, a => Assert.InRange(a, short.MinValue, short.MaxValue));
    }

    [Fact]
    public void CabecalhoEPreservado()
    {
        var wav = MontarWav(100, 200, 300);

        var saida = SoundService.ApplyVolume(wav, 0.5);

        Assert.Equal(wav.Length, saida.Length);
        Assert.Equal(wav.Take(44).ToArray(), saida.Take(44).ToArray());
    }

    [Fact]
    public void DadosNaoReconhecidos_SaoDevolvidosIntactos()
    {
        // Nao e um RIFF: a funcao nao deve corromper nem lancar excecao.
        var lixo = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };

        var saida = SoundService.ApplyVolume(lixo, 0.5);

        Assert.Equal(lixo, saida);
    }

    [Theory]
    [InlineData("CronoAula.Assets.alerta.wav")]
    [InlineData("CronoAula.Assets.aviso.wav")]
    public void OsSonsEmbutidosExistemESaoValidos(string recurso)
    {
        // Garante que os recursos foram realmente embutidos no assembly: se o nome
        // do arquivo ou do namespace mudar, este teste avisa antes do usuario notar
        // que o som sumiu no executavel unico.
        var asm = typeof(SoundService).Assembly;
        using var stream = asm.GetManifestResourceStream(recurso);

        Assert.NotNull(stream);

        using var ms = new MemoryStream();
        stream!.CopyTo(ms);
        var wav = ms.ToArray();

        Assert.True(wav.Length > 44, $"{recurso} parece vazio.");
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(wav, 0, 4));
        Assert.Equal("WAVE", System.Text.Encoding.ASCII.GetString(wav, 8, 4));
    }

    [Fact]
    public void OAlertaDeFimEDiferenteDoAvisoAntecipado()
    {
        // O professor precisa distinguir "esta acabando" de "acabou" so pelo som.
        var asm = typeof(SoundService).Assembly;

        static byte[] Ler(System.Reflection.Assembly a, string nome)
        {
            using var s = a.GetManifestResourceStream(nome)!;
            using var m = new MemoryStream();
            s.CopyTo(m);
            return m.ToArray();
        }

        var alerta = Ler(asm, "CronoAula.Assets.alerta.wav");
        var aviso = Ler(asm, "CronoAula.Assets.aviso.wav");

        Assert.NotEqual(alerta, aviso);
        // O alerta de fim e visivelmente mais longo: e um padrao, nao um bipe.
        Assert.True(alerta.Length > aviso.Length * 2,
            "O alerta de fim deveria ser bem mais longo que o aviso antecipado.");
    }

    [Fact]
    public void AlertaDesligado_NaoInicia()
    {
        using var s = new SoundService();

        s.StartAlert(habilitado: false, volume: 0.5, repeticoes: 5, intervaloSegundos: 3);

        Assert.False(s.AlertaAtivo);
    }

    [Fact]
    public void VolumeZero_NaoInicia()
    {
        using var s = new SoundService();

        s.StartAlert(habilitado: true, volume: 0.0, repeticoes: 5, intervaloSegundos: 3);

        Assert.False(s.AlertaAtivo);
    }

    [Fact]
    public void RepeticaoUnica_NaoDeixaSequenciaPendente()
    {
        using var s = new SoundService();

        // Uma repeticao so: toca na hora e nao agenda nada.
        s.StartAlert(habilitado: true, volume: 0.4, repeticoes: 1, intervaloSegundos: 3);

        Assert.False(s.AlertaAtivo);
    }

    [Fact]
    public void VariasRepeticoes_FicamPendentes_ATeSeremParadas()
    {
        using var s = new SoundService();

        s.StartAlert(habilitado: true, volume: 0.4, repeticoes: 5, intervaloSegundos: 5);
        Assert.True(s.AlertaAtivo);

        s.StopAlert();
        Assert.False(s.AlertaAtivo);
    }

    [Fact]
    public void RepeticaoZero_SignificaAteParar()
    {
        using var s = new SoundService();

        // 0 = repetir ate alguem interromper; deve ficar ativo.
        s.StartAlert(habilitado: true, volume: 0.4, repeticoes: 0, intervaloSegundos: 5);

        Assert.True(s.AlertaAtivo);
        s.StopAlert();
    }

    [Fact]
    public void PararDuasVezes_NaoLancaExcecao()
    {
        using var s = new SoundService();

        s.StartAlert(habilitado: true, volume: 0.4, repeticoes: 5, intervaloSegundos: 5);
        s.StopAlert();
        s.StopAlert(); // idempotente

        Assert.False(s.AlertaAtivo);
    }

    [Fact]
    public void NovoAlerta_SubstituiOAnterior()
    {
        using var s = new SoundService();

        s.StartAlert(habilitado: true, volume: 0.4, repeticoes: 5, intervaloSegundos: 5);
        s.StartAlert(habilitado: true, volume: 0.4, repeticoes: 3, intervaloSegundos: 5);

        Assert.True(s.AlertaAtivo);
        s.StopAlert();
    }

    [Fact]
    public void AposDispose_NaoIniciaMais()
    {
        var s = new SoundService();
        s.Dispose();

        s.StartAlert(habilitado: true, volume: 0.4, repeticoes: 5, intervaloSegundos: 5);

        Assert.False(s.AlertaAtivo);
    }
}
