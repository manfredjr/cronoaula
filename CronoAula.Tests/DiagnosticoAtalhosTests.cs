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
        //
        // A verificacao compara duas passadas seguidas em vez de exigir que
        // nada apareca como ocupado. Assim o teste continua valido mesmo com o
        // CronoAula aberto ou com outro programa segurando alguma combinacao:
        // o que importa e que a primeira passada nao mude o resultado da
        // segunda.
        var primeira = DiagnosticoAtalhos.Gerar(new AppSettings());
        var segunda = DiagnosticoAtalhos.Gerar(new AppSettings());

        Assert.Equal(primeira, segunda);
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

/// <summary>
/// Cada teste usa um sufixo proprio no nome da trava.
///
/// Sem isso, os testes disputariam o mesmo mutex do aplicativo e falhariam
/// sempre que o CronoAula estivesse aberto na maquina. Aconteceu de verdade
/// durante o desenvolvimento: quatro testes quebraram por causa de uma janela
/// esquecida, e nao por causa do codigo.
/// </summary>
public class InstanciaUnicaTests
{
    private static string NovoSufixo() => "teste-" + Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public void PrimeiraCopia_Assume_SegundaNao()
    {
        var sufixo = NovoSufixo();

        using var primeira = new InstanciaUnica(sufixo);
        Assert.True(primeira.TentarAssumir());

        using (var segunda = new InstanciaUnica(sufixo))
        {
            // Enquanto a primeira estiver viva, a segunda precisa recuar.
            Assert.False(segunda.TentarAssumir());
        }
    }

    [Fact]
    public void TravasComNomesDiferentes_NaoSeAtrapalham()
    {
        // Garante que o sufixo realmente isola: sem isso os testes acima
        // passariam por acidente.
        using var uma = new InstanciaUnica(NovoSufixo());
        using var outra = new InstanciaUnica(NovoSufixo());

        Assert.True(uma.TentarAssumir());
        Assert.True(outra.TentarAssumir());
    }

    [Fact]
    public void AposLiberar_OutraCopiaConsegueAssumir()
    {
        var sufixo = NovoSufixo();

        var primeira = new InstanciaUnica(sufixo);
        Assert.True(primeira.TentarAssumir());
        primeira.Dispose();

        using var segunda = new InstanciaUnica(sufixo);
        Assert.True(segunda.TentarAssumir());
    }

    [Fact]
    public void SegundaCopia_AvisaAPrimeira()
    {
        var sufixo = NovoSufixo();

        using var primeira = new InstanciaUnica(sufixo);
        Assert.True(primeira.TentarAssumir());

        var avisada = new ManualResetEventSlim(false);
        primeira.PedidoDeMostrarJanela += (_, _) => avisada.Set();
        primeira.EscutarPedidos();

        // Simula a segunda copia pedindo que a primeira apareca.
        InstanciaUnica.PedirParaMostrarJanelaExistente(sufixo);

        Assert.True(avisada.Wait(TimeSpan.FromSeconds(5)),
            "A primeira copia nao recebeu o aviso da segunda.");
    }

    [Fact]
    public void PedidoSemNinguemEscutando_NaoLanca()
    {
        // A segunda copia nao pode quebrar se a primeira ja tiver encerrado.
        var excecao = Record.Exception(
            () => InstanciaUnica.PedirParaMostrarJanelaExistente(NovoSufixo()));

        Assert.Null(excecao);
    }
}
