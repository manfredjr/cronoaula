namespace CronoAula.Tests;

/// <summary>
/// Protege a pasta docs/, que e a raiz publica do site em cronoaula.escalada.dev.
///
/// Nos outros projetos da conta, docs/ guarda documentacao interna. Aqui nao:
/// o cPanel serve essa pasta direto na internet. Uma anotacao, um script ou um
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

    /// <summary>
    /// Arquivos sem extensao que tem papel conhecido. O CNAME e do GitHub Pages
    /// e sai quando o Pages for desligado.
    /// </summary>
    private static readonly HashSet<string> NomesPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "CNAME"
    };

    private static string PastaDoSite()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var candidato = Path.Combine(dir, "docs");
            if (File.Exists(Path.Combine(candidato, "index.html")))
                return candidato;
            dir = Path.GetDirectoryName(dir);
        }

        throw new DirectoryNotFoundException("docs/index.html nao foi encontrado a partir de " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Docs_TemSoArquivosDeSite()
    {
        var raiz = PastaDoSite();

        var intrusos = Directory.EnumerateFiles(raiz, "*", SearchOption.AllDirectories)
            .Where(f =>
            {
                var nome = Path.GetFileName(f);
                return !NomesPermitidos.Contains(nome)
                       && !ExtensoesDeSite.Contains(Path.GetExtension(f));
            })
            .Select(f => Path.GetRelativePath(raiz, f))
            .ToList();

        Assert.True(intrusos.Count == 0,
            "Arquivos que nao sao de site dentro de docs/, a raiz publica: "
            + string.Join(", ", intrusos)
            + ". Documentacao interna vai na raiz do repositorio, nunca em docs/.");
    }

    [Fact]
    public void Docs_NaoTemArquivoDeSegredo()
    {
        // Redundante com o teste acima de proposito: estes nomes nao podem
        // escapar nem se alguem ampliar a lista de extensoes permitidas.
        var raiz = PastaDoSite();
        var proibidos = new[] { ".env", "config.php", "credenciais.php", ".htpasswd" };

        foreach (var nome in proibidos)
            Assert.False(File.Exists(Path.Combine(raiz, nome)), $"{nome} dentro de docs/ ficaria publico.");
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
}
