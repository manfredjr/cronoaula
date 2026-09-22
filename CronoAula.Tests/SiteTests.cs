using System.Text.RegularExpressions;

namespace CronoAula.Tests;

/// <summary>
/// Protege a pasta public/, raiz do site em cronoaula.escalada.dev.
///
/// O cPanel serve essa pasta direto na internet. Uma anotacao, um script ou um
/// arquivo de configuracao deixado ali fica publico no primeiro deploy. Estes
/// testes falham antes disso acontecer.
/// </summary>
public class SiteTests
{
    /// <summary>Extensoes que fazem sentido numa pagina estatica.</summary>
    private static readonly HashSet<string> ExtensoesDeSite = new(StringComparer.OrdinalIgnoreCase)
    {
        ".html", ".css", ".js", ".ico", ".png", ".svg", ".jpg", ".jpeg", ".webp",
        ".txt", ".xml", ".webmanifest", ".wav"
    };

    private static string PastaDoSite()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var candidato = Path.Combine(dir, "public");
            if (File.Exists(Path.Combine(candidato, "index.html")))
                return candidato;
            dir = Path.GetDirectoryName(dir);
        }

        throw new DirectoryNotFoundException("public/index.html nao foi encontrado a partir de " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Public_TemSoArquivosDeSite()
    {
        var raiz = PastaDoSite();

        var intrusos = Directory.EnumerateFiles(raiz, "*", SearchOption.AllDirectories)
            .Where(f => !ExtensoesDeSite.Contains(Path.GetExtension(f)))
            .Select(f => Path.GetRelativePath(raiz, f))
            .ToList();

        Assert.True(intrusos.Count == 0,
            "Arquivos que nao sao de site dentro de public/, a raiz publica: "
            + string.Join(", ", intrusos)
            + ". Documentacao interna vai na raiz do repositorio, nunca em public/.");
    }

    [Fact]
    public void Public_NaoTemArquivoDeSegredo()
    {
        // Redundante com o teste acima de proposito: estes nomes nao podem
        // escapar nem se alguem ampliar a lista de extensoes permitidas.
        var raiz = PastaDoSite();
        var proibidos = new[] { ".env", "config.php", "credenciais.php", ".htpasswd" };

        foreach (var nome in proibidos)
            Assert.False(File.Exists(Path.Combine(raiz, nome)), $"{nome} dentro de public/ ficaria publico.");
    }

    [Fact]
    public void Site_AnunciaOEnderecoNovo()
    {
        var html = File.ReadAllText(Path.Combine(PastaDoSite(), "index.html"));

        Assert.Contains("<link rel=\"canonical\" href=\"https://cronoaula.escalada.dev/\">", html);
        Assert.Contains("content=\"https://cronoaula.escalada.dev/\"", html);
    }

    [Fact]
    public void Site_BaixaOExecutavelDoGitHub()
    {
        // O executavel nunca vai para o servidor: fica no GitHub Releases, e o
        // site aponta para la. Se este link sumir, o botao de download quebra.
        var html = File.ReadAllText(Path.Combine(PastaDoSite(), "index.html"));

        Assert.Contains("https://github.com/manfredjr/cronoaula/releases/latest/download/CronoAula.exe", html);
    }

    [Fact]
    public void Rodape_SegueOPadraoDoEcossistema()
    {
        // O rodape e o mesmo dos produtos do Escalada.dev (UniTask, ChamaAula):
        // credito da MT e da plataforma, com os dois links, e o logo ao lado.
        var raiz = PastaDoSite();
        var html = File.ReadAllText(Path.Combine(raiz, "index.html"));

        Assert.Contains("Desenvolvido e publicado por", html);
        Assert.Contains("href=\"https://www.manfred.com.br\"", html);
        Assert.Contains("href=\"https://escalada.dev\"", html);
        Assert.Contains("src=\"logo-mt.png\"", html);
        Assert.True(File.Exists(Path.Combine(raiz, "logo-mt.png")), "public/logo-mt.png nao existe.");
    }

    [Fact]
    public void Rodape_MantemONomeDaLicenca()
    {
        // Divergencia consciente em relacao ao ChamaAula, registrada no CLAUDE.md
        // dele: o CronoAula e distribuido de verdade, sob GPL-3.0, entao o nome da
        // licenca continua no rodape.
        var html = File.ReadAllText(Path.Combine(PastaDoSite(), "index.html"));
        var inicio = html.IndexOf("<footer", StringComparison.Ordinal);
        var rodape = html[inicio..];

        Assert.Contains("GPL-3.0", rodape);
    }

    [Fact]
    public void PastaDocsAntiga_NaoVoltou()
    {
        // O site ficou em docs/ enquanto era servido pelo GitHub Pages. Uma
        // pasta docs/ com index.html de volta indicaria duas copias do site.
        var raiz = Path.GetDirectoryName(PastaDoSite())!;

        Assert.False(File.Exists(Path.Combine(raiz, "docs", "index.html")),
            "docs/index.html reapareceu; o site agora fica so em public/.");
    }

    private static string PastaUsar() => Path.Combine(PastaDoSite(), "usar");

    [Fact]
    public void Usar_PaginaExiste()
    {
        Assert.True(File.Exists(Path.Combine(PastaUsar(), "index.html")), "public/usar/index.html nao existe.");
    }

    [Fact]
    public void Usar_TodoArquivoCarregadoExiste()
    {
        // A pagina e os modulos se referem uns aos outros por caminho relativo.
        // Um nome errado so apareceria no navegador, como tela parada.
        var raiz = PastaDoSite();
        var faltando = new List<string>();

        void Conferir(string origem, string referencia)
        {
            if (referencia.StartsWith("http", StringComparison.Ordinal) || referencia.StartsWith('#') || referencia.StartsWith("mailto:", StringComparison.Ordinal))
                return;

            var caminho = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(origem)!, referencia.Replace('/', Path.DirectorySeparatorChar)));
            if (referencia.EndsWith('/'))
                caminho = Path.Combine(caminho, "index.html");

            if (!File.Exists(caminho))
                faltando.Add($"{Path.GetRelativePath(raiz, origem)} -> {referencia}");
        }

        var html = Path.Combine(PastaUsar(), "index.html");
        foreach (Match m in Regex.Matches(File.ReadAllText(html), "(?:src|href)=\"([^\"]+)\""))
            Conferir(html, m.Groups[1].Value);

        foreach (var js in Directory.EnumerateFiles(Path.Combine(PastaUsar(), "js"), "*.js"))
        {
            var texto = File.ReadAllText(js);
            foreach (Match m in Regex.Matches(texto, "from '([^']+)'"))
                Conferir(js, m.Groups[1].Value);
            foreach (Match m in Regex.Matches(texto, @"new URL\('([^']+)', import\.meta\.url\)"))
                Conferir(js, m.Groups[1].Value);
        }

        Assert.True(faltando.Count == 0, "Referencias quebradas: " + string.Join(", ", faltando));
    }

    [Theory]
    [InlineData("alerta.wav")]
    [InlineData("aviso.wav")]
    public void Usar_SonsSaoOsMesmosDoPrograma(string nome)
    {
        var raiz = PastaDoSite();
        var web = File.ReadAllBytes(Path.Combine(raiz, "usar", "sons", nome));
        var programa = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(raiz)!, "CronoAula", "Assets", nome));

        Assert.True(web.AsSpan().SequenceEqual(programa),
            $"public/usar/sons/{nome} difere de CronoAula/Assets/{nome}. Copie o do programa de novo.");
    }

    [Fact]
    public void Public_NaoTemArquivoDeTeste()
    {
        var raiz = PastaDoSite();
        var testes = Directory.EnumerateFiles(raiz, "*.test.*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(raiz, f))
            .ToList();

        Assert.True(testes.Count == 0, "Arquivos de teste dentro de public/: " + string.Join(", ", testes) + ". Eles ficam em web-testes/.");
    }

    [Fact]
    public void Usar_NaoCarregaNadaDeFora()
    {
        // Promessa de "sem rede e sem telemetria": a pagina nao busca nada em
        // outro endereco. Links clicaveis no rodape nao contam, sao navegacao.
        var pasta = PastaUsar();

        var html = File.ReadAllText(Path.Combine(pasta, "index.html"));
        Assert.DoesNotMatch("src=\"(https?:)?//", html);
        Assert.DoesNotMatch("<link[^>]+rel=\"(stylesheet|preload|modulepreload|preconnect)\"[^>]+href=\"(https?:)?//", html);

        var css = File.ReadAllText(Path.Combine(pasta, "usar.css"));
        Assert.DoesNotContain("@import", css);
        Assert.DoesNotMatch(@"url\(\s*['""]?(https?:)?//", css);

        foreach (var js in Directory.EnumerateFiles(Path.Combine(pasta, "js"), "*.js"))
            Assert.DoesNotMatch("https?://", File.ReadAllText(js));
    }

    [Fact]
    public void Usar_RodapeSegueOPadraoDoEcossistema()
    {
        var html = File.ReadAllText(Path.Combine(PastaUsar(), "index.html"));

        Assert.Contains("Desenvolvido e publicado por", html);
        Assert.Contains("href=\"https://www.manfred.com.br\"", html);
        Assert.Contains("href=\"https://escalada.dev\"", html);
        Assert.Contains("src=\"../logo-mt.png\"", html);
        Assert.Contains("GPL-3.0", html[html.IndexOf("<footer", StringComparison.Ordinal)..]);
    }

    [Fact]
    public void Abertura_OfereceOsDoisCaminhos()
    {
        // Opcao B da maquete: navegador em destaque (botao cheio), Windows vazado.
        var html = File.ReadAllText(Path.Combine(PastaDoSite(), "index.html"));

        Assert.Matches("<a class=\"btn btn-main\" href=\"usar/\">\\s*Usar no navegador\\s*</a>", html);
        Assert.Matches("<a class=\"btn btn-alt\" href=\"https://github.com/manfredjr/cronoaula/releases/latest/download/CronoAula.exe\">\\s*Baixar para Windows\\s*</a>", html);
        Assert.Contains("fica por cima do PowerPoint", html);
    }
}
