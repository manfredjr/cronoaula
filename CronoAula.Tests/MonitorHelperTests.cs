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
        var resolvido = MonitorHelper.Resolver("\\\\.\\DISPLAY_QUE_NAO_EXISTE", IntPtr.Zero);

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
