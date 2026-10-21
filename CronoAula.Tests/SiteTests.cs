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

    /// <summary>
    /// Arquivos sem extensao que fazem parte do site. O .htaccess configura o
    /// Apache do cPanel e precisa estar na raiz publica para valer.
    /// </summary>
    private static readonly HashSet<string> ArquivosDeConfiguracao = new(StringComparer.OrdinalIgnoreCase)
    {
        ".htaccess"
    };

    /// <summary>Enderecos de fora que a pagina pode carregar, e so eles.</summary>
    private static readonly string[] EnderecosDoGoogleAnalytics =
    {
        "https://www.googletagmanager.com",
        "https://www.google-analytics.com"
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
            .Where(f => !ExtensoesDeSite.Contains(Path.GetExtension(f))
                        && !ArquivosDeConfiguracao.Contains(Path.GetFileName(f)))
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

        // Manual da marca: sobre fundo escuro, o logo vai com o texto em branco,
        // sem caixa clara atras.
        Assert.Contains("<source srcset=\"logo-mt-claro.svg\" media=\"(prefers-color-scheme: dark)\">", html);
        Assert.True(File.Exists(Path.Combine(raiz, "logo-mt-claro.svg")), "public/logo-mt-claro.svg nao existe.");
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

            // A ancora e o parametro nao fazem parte do nome do arquivo:
            // "privacidade.html#medicao" aponta para privacidade.html.
            referencia = referencia.Split('#')[0].Split('?')[0];
            if (referencia.Length == 0)
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
        // A pagina e sempre escura: vale a versao do logo com o texto em branco.
        Assert.Contains("src=\"../logo-mt-claro.svg\"", html);
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

    [Fact]
    public void Deploy_ConfereAVersaoWeb()
    {
        // O deploy do cPanel para antes de publicar se a pagina da versao web sumir.
        var raiz = Path.GetDirectoryName(PastaDoSite())!;
        var yml = File.ReadAllText(Path.Combine(raiz, ".cpanel.yml"));

        Assert.Contains("test -f $REPO/public/usar/index.html", yml);
        Assert.Contains("test -f $REPO/public/privacidade.html", yml);
        Assert.Contains("test -f $REPO/public/medicao.js", yml);
    }

    // ------------------------------------------------------------------ medicao

    private static string TextoDaMedicao() => File.ReadAllText(Path.Combine(PastaDoSite(), "medicao.js"));

    private static IEnumerable<string> PaginasDoSite()
    {
        var raiz = PastaDoSite();
        yield return Path.Combine(raiz, "index.html");
        yield return Path.Combine(raiz, "usar", "index.html");
        yield return Path.Combine(raiz, "privacidade.html");
    }

    [Fact]
    public void Medicao_NenhumaPaginaTrazATagDoGoogle()
    {
        // O coracao da promessa: antes do aceite, nenhuma requisicao vai ao
        // Google. A tag nao fica no HTML; quem a injeta e o medicao.js, e so
        // depois do clique em Aceitar.
        foreach (var pagina in PaginasDoSite())
        {
            var html = File.ReadAllText(pagina);
            var nome = Path.GetFileName(Path.GetDirectoryName(pagina)) + "/" + Path.GetFileName(pagina);

            Assert.False(html.Contains("googletagmanager", StringComparison.OrdinalIgnoreCase),
                $"{nome} traz a tag do Google no HTML, entao ela carregaria sem aceite.");
            Assert.False(html.Contains("gtag(", StringComparison.OrdinalIgnoreCase),
                $"{nome} tem codigo do gtag embutido.");
            Assert.DoesNotMatch("src=\"(https?:)?//", html);
        }
    }

    [Fact]
    public void Medicao_SoFalaComOGoogleAnalytics()
    {
        // Fora os enderecos do Google Analytics, a pagina nao busca nada de
        // terceiro. Vale para todo JavaScript e todo CSS da pasta publica.
        var raiz = PastaDoSite();
        var medicao = TextoDaMedicao();

        foreach (Match m in Regex.Matches(medicao, @"https?://[^\s'""()]+"))
        {
            var endereco = m.Value;
            Assert.True(EnderecosDoGoogleAnalytics.Any(e => endereco.StartsWith(e, StringComparison.Ordinal)),
                $"medicao.js busca {endereco}, que nao e do Google Analytics.");
        }

        foreach (var js in Directory.EnumerateFiles(raiz, "*.js", SearchOption.AllDirectories)
                     .Where(f => !string.Equals(Path.GetFileName(f), "medicao.js", StringComparison.OrdinalIgnoreCase)))
            Assert.DoesNotMatch("https?://", File.ReadAllText(js));

        foreach (var css in Directory.EnumerateFiles(raiz, "*.css", SearchOption.AllDirectories))
        {
            var texto = File.ReadAllText(css);
            Assert.DoesNotContain("@import", texto);
            Assert.DoesNotMatch(@"url\(\s*['""]?(https?:)?//", texto);
        }
    }

    [Fact]
    public void Medicao_AvisoTemOsDoisCaminhosEContaDaTransferencia()
    {
        // O aviso e o momento do consentimento. A LGPD, art. 33, VIII, exige
        // informacao previa sobre o carater internacional da transferencia no
        // proprio pedido, e nao so na politica.
        var medicao = TextoDaMedicao();

        Assert.Contains("Medição de audiência", medicao);
        Assert.Contains("Google Analytics", medicao);
        Assert.Contains("Google LLC", medicao);
        Assert.Contains("fora do Brasil", medicao);
        Assert.Contains("nada é enviado e o site funciona igual", medicao);
        Assert.Contains("Como isso funciona", medicao);
        Assert.Contains("Aceitar", medicao);
        Assert.Contains("Recusar", medicao);
    }

    [Fact]
    public void Medicao_GuardaAEscolhaSoNoNavegador()
    {
        // Uma chave so no localStorage, sem prazo e sem cookie: a escolha nao
        // vai ao servidor nem atravessa subdominio.
        var medicao = TextoDaMedicao();

        Assert.Contains("cronoaula.medicao", medicao);
        Assert.Contains("localStorage", medicao);
        Assert.DoesNotContain("document.cookie", medicao);
    }

    [Fact]
    public void Medicao_NaoMandaOEnderecoSujoNemPersonalizaAnuncio()
    {
        // O Google recebe o caminho limpo. Se um dia alguma tela usar token ou
        // parametro, ele nao vai junto.
        var medicao = TextoDaMedicao();

        Assert.Contains("page_location", medicao);
        Assert.Contains("page_referrer", medicao);
        Assert.Contains("page_title", medicao);
        Assert.Contains("allow_google_signals", medicao);
        Assert.Contains("allow_ad_personalization_signals", medicao);
        // Sem o modo de consentimento do Google, que mede tambem quem recusou.
        Assert.DoesNotContain("'consent'", medicao);
    }

    [Fact]
    public void Medicao_TodaPaginaDeixaReverAEscolha()
    {
        // Revogar o consentimento tem que ser gratuito e facilitado
        // (LGPD, art. 8º, § 5º): o link fica no rodape de todas as paginas.
        foreach (var pagina in PaginasDoSite())
        {
            var html = File.ReadAllText(pagina);
            Assert.Contains("Rever a escolha de medição", html);
            Assert.Contains("rever-medicao", html);
        }
    }

    [Fact]
    public void Privacidade_TemOQueALeiPede()
    {
        // LGPD, art. 9º: finalidade, duracao, quem e o controlador, contato,
        // com quem os dados sao compartilhados e os direitos do art. 18.
        var html = File.ReadAllText(Path.Combine(PastaDoSite(), "privacidade.html"));

        Assert.Contains("MANFRED TECNOLOGIA LTDA", html);
        Assert.Contains("21.075.901/0001-12", html);
        Assert.Contains("manfred@manfred.com.br", html);
        Assert.Contains("id=\"medicao\"", html);
        Assert.Contains("Google LLC", html);
        Assert.Contains("transferência internacional", html);
        Assert.Contains("consentimento", html);
        Assert.Contains("dois meses", html);
        Assert.Contains("art. 18", html);

        // Revogar vale daqui para a frente, e a pagina precisa dizer isso
        // (LGPD, art. 8º, § 5º).
        Assert.Contains("A revogação vale daqui para a frente", html);

        // O que o site promete nao coletar continua escrito, para a pessoa ver
        // a diferenca entre o programa e a pagina.
        Assert.Contains("Não há publicidade", html);
    }

    [Fact]
    public void Politica_DeConteudoLiberaSoOGoogleAnalytics()
    {
        var raiz = PastaDoSite();
        var htaccess = File.ReadAllText(Path.Combine(raiz, ".htaccess"));

        // So a politica em si, e nao os comentarios que explicam o arquivo.
        var linha = Regex.Match(htaccess, "Header set Content-Security-Policy \"([^\"]+)\"");
        Assert.True(linha.Success, "O .htaccess nao define a Content-Security-Policy.");
        var politica = linha.Groups[1].Value;

        Assert.Contains("script-src 'self' https://www.googletagmanager.com", politica);
        Assert.Contains("https://www.google-analytics.com", politica);
        Assert.Contains("https://*.analytics.google.com", politica);
        Assert.Contains("object-src 'none'", politica);

        // Sem curinga solto e sem unsafe-eval, como combinado com a Escalada.
        Assert.DoesNotContain("unsafe-eval", politica);
        Assert.DoesNotMatch(@"-src[^;]*\s\*[\s;]", politica);
    }
}
