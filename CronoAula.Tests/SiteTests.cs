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
        ".txt", ".xml", ".webmanifest"
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
    public void PastaDocsAntiga_NaoVoltou()
    {
        // O site ficou em docs/ enquanto era servido pelo GitHub Pages. Uma
        // pasta docs/ com index.html de volta indicaria duas copias do site.
        var raiz = Path.GetDirectoryName(PastaDoSite())!;

        Assert.False(File.Exists(Path.Combine(raiz, "docs", "index.html")),
            "docs/index.html reapareceu; o site agora fica so em public/.");
    }
}
