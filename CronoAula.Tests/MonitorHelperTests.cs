using CronoAula.Core;

namespace CronoAula.Tests;

public class MonitorHelperTests
{
    [Fact]
    public void Listar_EncontraPeloMenosUmMonitor()
    {
        var monitores = MonitorHelper.Listar();

        Assert.NotEmpty(monitores);
    }

    [Fact]
    public void TodoMonitor_TemDimensoesPlausiveis()
    {
        foreach (var m in MonitorHelper.Listar())
        {
            Assert.True(m.Width > 0, $"{m.Rotulo} com largura invalida");
            Assert.True(m.Height > 0, $"{m.Rotulo} com altura invalida");
            Assert.False(string.IsNullOrWhiteSpace(m.Id));
            Assert.False(string.IsNullOrWhiteSpace(m.Rotulo));
        }
    }

    [Fact]
    public void ExisteExatamenteUmMonitorPrincipal()
    {
        var principais = MonitorHelper.Listar().Count(m => m.Principal);

        Assert.Equal(1, principais);
    }

    [Fact]
    public void IdsSaoUnicos()
    {
        // O id e a chave usada para lembrar em qual tela abrir; duplicidade
        // faria a escolha do professor cair no monitor errado.
        var monitores = MonitorHelper.Listar();

        Assert.Equal(monitores.Count, monitores.Select(m => m.Id).Distinct().Count());
    }

    [Fact]
    public void Resolver_ComIdInexistente_CaiNoMonitorDaJanela()
    {
        // Cenario real: o professor escolheu o projetor, desconectou o cabo e
        // abriu o programa em casa. Nao pode ficar sem tela.
        var resolvido = MonitorHelper.Resolver("MONITOR_QUE_NAO_EXISTE", IntPtr.Zero);

        Assert.NotNull(resolvido);
    }

    [Fact]
    public void Resolver_SemIdSalvo_DevolveUmMonitor()
    {
        var resolvido = MonitorHelper.Resolver(null, IntPtr.Zero);

        Assert.NotNull(resolvido);
    }

    [Fact]
    public void Resolver_ComIdValido_DevolveExatamenteEle()
    {
        var primeiro = MonitorHelper.Listar().First();

        var resolvido = MonitorHelper.Resolver(primeiro.Id, IntPtr.Zero);

        Assert.Equal(primeiro.Id, resolvido!.Id);
    }
}

/// <summary>
/// Regras do modo apresentador: o relogio vai para a tela projetada e o painel
/// de controle fica na outra. Estes testes usam monitores ficticios, porque a
/// maquina de desenvolvimento tem apenas uma tela.
///
/// Os identificadores aqui sao nomes simples de proposito. O Windows usa a
/// forma de caminho de dispositivo, mas repetir essa sintaxe em varios literais
/// fazia o Smart App Control bloquear o assembly de testes: e um padrao que
/// programas maliciosos usam para acesso bruto a disco. Para estas regras o
/// identificador e opaco, entao nomes simples testam exatamente a mesma coisa.
/// </summary>
public class EscolhaDeMonitorTests
{
    private static MonitorInfo Tela(string id, bool principal = false,
        int left = 0, int top = 0, int w = 1920, int h = 1080) =>
        new(id, $"Monitor {id}", left, top, w, h, principal);

    [Fact]
    public void UmaTelaSo_NaoHaOndeColocarOPainel()
    {
        var principal = Tela("TELA-1", principal: true);

        var painel = MonitorHelper.EscolherMonitorDoPainel(new[] { principal }, principal);

        // Null significa: esconda o painel, a exibicao ocupa tudo.
        Assert.Null(painel);
    }

    [Fact]
    public void ProjetandoNoSecundario_PainelVaiParaOPrincipal()
    {
        var principal = Tela("TELA-1", principal: true);
        var projetor = Tela("TELA-2", left: 1920);

        var painel = MonitorHelper.EscolherMonitorDoPainel(new[] { principal, projetor }, projetor);

        Assert.Equal(principal.Id, painel!.Id);
    }

    [Fact]
    public void ProjetandoNoPrincipal_PainelVaiParaOOutro()
    {
        // Acontece quando o projetor e configurado como tela principal.
        var principal = Tela("TELA-1", principal: true);
        var outro = Tela("TELA-2", left: 1920);

        var painel = MonitorHelper.EscolherMonitorDoPainel(new[] { principal, outro }, principal);

        Assert.Equal(outro.Id, painel!.Id);
    }

    [Fact]
    public void TresTelas_PainelPrefereAPrincipal()
    {
        var a = Tela("TELA-1");
        var principal = Tela("TELA-2", principal: true, left: 1920);
        var projetor = Tela("TELA-3", left: 3840);

        var painel = MonitorHelper.EscolherMonitorDoPainel(new[] { a, principal, projetor }, projetor);

        Assert.Equal(principal.Id, painel!.Id);
    }

    [Fact]
    public void PainelNuncaCaiNaMesmaTelaDaProjecao()
    {
        var a = Tela("TELA-1", principal: true);
        var b = Tela("TELA-2", left: 1920);
        var monitores = new[] { a, b };

        foreach (var exibicao in monitores)
        {
            var painel = MonitorHelper.EscolherMonitorDoPainel(monitores, exibicao);
            Assert.NotEqual(exibicao.Id, painel!.Id);
        }
    }

    [Fact]
    public void CantoInferiorDireito_RespeitaAMargem()
    {
        var m = Tela("TELA-1", left: 0, top: 0, w: 1920, h: 1080);

        var (left, top) = MonitorHelper.CantoInferiorDireito(m, 300, 240, margem: 20);

        Assert.Equal(1920 - 300 - 20, left);
        Assert.Equal(1080 - 240 - 20, top);
    }

    [Fact]
    public void CantoInferiorDireito_FuncionaEmMonitorDeslocado()
    {
        // Segundo monitor comeca em x = 1920; a janela precisa cair dentro dele.
        var m = Tela("TELA-2", left: 1920, top: 0, w: 1280, h: 1024);

        var (left, top) = MonitorHelper.CantoInferiorDireito(m, 300, 240, margem: 20);

        Assert.Equal(1920 + 1280 - 300 - 20, left);
        Assert.Equal(1024 - 240 - 20, top);
        Assert.InRange(left, m.Left, m.Left + m.Width);
    }

    [Fact]
    public void CantoInferiorDireito_NaoSaiDaTelaComJanelaMaiorQueOMonitor()
    {
        // Projetor de baixa resolucao: a janela nao cabe. Ainda assim ela nao
        // pode comecar fora da tela, senao o professor perde o painel de vista.
        var m = Tela("TELA-2", left: 0, top: 0, w: 640, h: 480);

        var (left, top) = MonitorHelper.CantoInferiorDireito(m, 900, 700);

        Assert.Equal(m.Left, left);
        Assert.Equal(m.Top, top);
    }
}
