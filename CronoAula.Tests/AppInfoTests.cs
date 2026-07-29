using System.Reflection;
using CronoAula.Core;

namespace CronoAula.Tests;

/// <summary>
/// Estes testes existem por causa de uma falha real: a versao 1.0.0 fechava
/// sozinha ao abrir a janela "Sobre" no executavel unico, porque lia a versao
/// via Assembly.Location - que vem VAZIO nesse modo de publicacao.
/// </summary>
public class AppInfoTests
{
    [Fact]
    public void Versao_NaoEVaziaENaoLanca()
    {
        var v = AppInfo.Versao;

        Assert.False(string.IsNullOrWhiteSpace(v));
    }

    [Fact]
    public void Versao_TemFormatoDeVersao()
    {
        // Espera algo como "1.0.0" ou "1.0.0.0", nunca um caminho ou texto solto.
        Assert.Matches(@"^\d+(\.\d+)+$", AppInfo.Versao);
    }

    [Fact]
    public void Copyright_TrazOTitular()
    {
        var c = AppInfo.Copyright;

        Assert.Contains("Manfred Tecnologia", c);
    }

    [Fact]
    public void Copyright_UsaOSimbolo_NaoOTextoCru()
    {
        var c = AppInfo.Copyright;

        Assert.Contains("©", c);
        Assert.DoesNotContain("Copyright (c)", c);
    }

    [Fact]
    public void Produto_ECronoAula()
    {
        Assert.Equal("CronoAula", AppInfo.Produto);
    }

    /// <summary>
    /// Reproduz a condicao do arquivo unico: um assembly cujo Location e vazio.
    /// Assemblies gerados em memoria se comportam assim, que e exatamente o
    /// cenario que derrubava o programa.
    /// </summary>
    [Fact]
    public void AssemblySemArquivoEmDisco_NaoLanca()
    {
        var dinamico = AssemblyBuilderFake();

        Assert.Equal(string.Empty, dinamico.Location); // mesma condicao do arquivo unico

        // O que importa: nenhuma destas chamadas pode lancar excecao.
        var versao = AppInfo.LerVersao(dinamico);
        var copyright = AppInfo.LerCopyright(dinamico);
        var produto = AppInfo.LerProduto(dinamico);

        Assert.False(string.IsNullOrWhiteSpace(versao));
        Assert.NotNull(copyright);
        Assert.Equal("CronoAula", produto); // cai no padrao, sem quebrar
    }

    [Fact]
    public void SemAtributosDeVersao_CaiNoPadrao()
    {
        var dinamico = AssemblyBuilderFake();

        var versao = AppInfo.LerVersao(dinamico);

        // Sem atributos, usa a versao do nome do assembly ou o padrao.
        Assert.Matches(@"^\d+(\.\d+)+$", versao);
    }

    private static Assembly AssemblyBuilderFake()
    {
        // Assembly sem arquivo correspondente em disco: Location fica vazio.
        var nome = new AssemblyName("CronoAulaTesteDinamico") { Version = new Version(9, 9, 9, 9) };
        return System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
            nome, System.Reflection.Emit.AssemblyBuilderAccess.Run);
    }
}
