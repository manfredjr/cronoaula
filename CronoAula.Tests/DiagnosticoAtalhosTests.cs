using CronoAula.Core;

namespace CronoAula.Tests;

public class DiagnosticoAtalhosTests
{
    [Fact]
    public void Relatorio_CobreTodasAsAcoes()
    {
        var texto = DiagnosticoAtalhos.Gerar(new AppSettings());

        // Toda acao com atalho precisa aparecer no relatorio; caso contrario o
        // professor nao consegue diagnosticar aquela tecla.
        foreach (var acao in Enum.GetValues<HotkeyAction>())
        {
            var nome = GlobalHotkeyManager.Describe(acao);
            Assert.Contains(nome, texto);
        }
    }

    [Fact]
    public void Relatorio_MarcaCombinacaoInvalida()
    {
        var s = new AppSettings();
        s.Hotkeys[nameof(HotkeyAction.Zerar)] = "TeclaQueNaoExiste";

        var texto = DiagnosticoAtalhos.Gerar(s);

        Assert.Contains("não é uma combinação válida", texto);
    }

    [Fact]
    public void Relatorio_MarcaAtalhoDesligado()
    {
        var s = new AppSettings();
        s.Hotkeys[nameof(HotkeyAction.TelaCheia)] = "";

        var texto = DiagnosticoAtalhos.Gerar(s);

        Assert.Contains("desligado nas preferências", texto);
    }

    [Fact]
    public void Relatorio_LembraDeFecharOPrograma()
    {
        // Sem esse lembrete o resultado engana: com o CronoAula aberto, as
        // combinacoes aparecem ocupadas por ele mesmo.
        var texto = DiagnosticoAtalhos.Gerar(new AppSettings());

        Assert.Contains("Feche o CronoAula", texto);
    }

    [Fact]
    public void Relatorio_NaoDeixaCombinacaoRegistrada()
    {
        // O diagnostico registra e libera cada combinacao. Se esquecesse de
        // liberar, roubaria as teclas do proprio cronometro depois.
        DiagnosticoAtalhos.Gerar(new AppSettings());

        var segundaPassada = DiagnosticoAtalhos.Gerar(new AppSettings());

        Assert.DoesNotContain("OCUPADA", segundaPassada);
    }
}

public class HotkeyStatusTests
{
    [Theory]
    [InlineData(HotkeySituacao.Ativo, false)]
    [InlineData(HotkeySituacao.Desligado, false)]
    [InlineData(HotkeySituacao.EmUso, true)]
    [InlineData(HotkeySituacao.Invalido, true)]
    [InlineData(HotkeySituacao.Falhou, true)]
    public void Problema_SoParaSituacoesQueExigemAcao(HotkeySituacao situacao, bool esperado)
    {
        var s = new HotkeyStatus(HotkeyAction.Zerar, "Zerar", "Ctrl+Alt+R", situacao, null);

        Assert.Equal(esperado, s.Problema);
    }

    [Fact]
    public void Resumo_ExplicaEmUsoPorOutroPrograma()
    {
        var s = new HotkeyStatus(HotkeyAction.AdicionarMinuto, "Somar 1 minuto",
            "Ctrl+Alt+Up", HotkeySituacao.EmUso, null);

        Assert.Equal("em uso por outro programa", s.Resumo);
    }

    [Fact]
    public void Resumo_DizQuandoEstaFuncionando()
    {
        var s = new HotkeyStatus(HotkeyAction.IniciarPausar, "Iniciar / Pausar",
            "Ctrl+Alt+S", HotkeySituacao.Ativo, null);

        Assert.Equal("funcionando", s.Resumo);
    }
}

public class InstanciaUnicaTests
{
    [Fact]
    public void PrimeiraCopia_Assume_SegundaNao()
    {
        using var primeira = new InstanciaUnica();
        Assert.True(primeira.TentarAssumir());

        using (var segunda = new InstanciaUnica())
        {
            // Enquanto a primeira estiver viva, a segunda precisa recuar.
            Assert.False(segunda.TentarAssumir());
        }
    }

    [Fact]
    public void AposLiberar_OutraCopiaConsegueAssumir()
    {
        var primeira = new InstanciaUnica();
        Assert.True(primeira.TentarAssumir());
        primeira.Dispose();

        using var segunda = new InstanciaUnica();
        Assert.True(segunda.TentarAssumir());
    }

    [Fact]
    public void SegundaCopia_AvisaAPrimeira()
    {
        using var primeira = new InstanciaUnica();
        Assert.True(primeira.TentarAssumir());

        var avisada = new ManualResetEventSlim(false);
        primeira.PedidoDeMostrarJanela += (_, _) => avisada.Set();
        primeira.EscutarPedidos();

        // Simula a segunda copia pedindo que a primeira apareca.
        InstanciaUnica.PedirParaMostrarJanelaExistente();

        Assert.True(avisada.Wait(TimeSpan.FromSeconds(5)),
            "A primeira copia nao recebeu o aviso da segunda.");
    }

    [Fact]
    public void PedidoSemNinguemEscutando_NaoLanca()
    {
        // A segunda copia nao pode quebrar se a primeira ja tiver encerrado.
        var excecao = Record.Exception(InstanciaUnica.PedirParaMostrarJanelaExistente);

        Assert.Null(excecao);
    }
}
