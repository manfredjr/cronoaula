using System.Reflection;

namespace CronoAula.Core;

/// <summary>
/// Identificacao do programa (versao, autoria) lida dos metadados do assembly.
///
/// ATENCAO ao mexer aqui: nao use Assembly.Location nem FileVersionInfo.
/// Em um executavel publicado como arquivo unico, Assembly.Location devolve
/// string VAZIA, porque nao existe DLL solta em disco para apontar. Passar esse
/// valor para FileVersionInfo.GetVersionInfo lanca ArgumentException e derruba o
/// processo inteiro. Foi exatamente o que aconteceu na versao 1.0.0 ao abrir a
/// janela "Sobre".
///
/// Os atributos abaixo vivem dentro do proprio assembly e funcionam tanto no
/// build de depuracao quanto no arquivo unico publicado.
/// </summary>
public static class AppInfo
{
    /// <summary>Versao exibida ao usuario. Nunca lanca excecao.</summary>
    public static string Versao => LerVersao(typeof(AppInfo).Assembly);

    /// <summary>Aviso de direitos autorais, ja formatado para exibicao.</summary>
    public static string Copyright => LerCopyright(typeof(AppInfo).Assembly);

    /// <summary>Nome do produto.</summary>
    public static string Produto => LerProduto(typeof(AppInfo).Assembly);

    internal static string LerVersao(Assembly assembly)
    {
        try
        {
            // InformationalVersion pode trazer sufixos de build (ex.: "1.0.0+abc123");
            // ficamos apenas com a parte antes do "+".
            var informacional = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informacional))
            {
                var corte = informacional.IndexOf('+');
                return corte > 0 ? informacional[..corte] : informacional;
            }

            var arquivo = assembly
                .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

            if (!string.IsNullOrWhiteSpace(arquivo))
                return arquivo;

            return assembly.GetName().Version?.ToString() ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }

    internal static string LerCopyright(Assembly assembly)
    {
        try
        {
            var texto = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;

            if (string.IsNullOrWhiteSpace(texto))
                return "";

            // Nos metadados o texto vem como "Copyright (c) ..."; na tela fica
            // melhor com o simbolo.
            return texto.Replace("Copyright (c)", "©").Trim();
        }
        catch
        {
            return "";
        }
    }

    internal static string LerProduto(Assembly assembly)
    {
        try
        {
            var texto = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
            return string.IsNullOrWhiteSpace(texto) ? "CronoAula" : texto;
        }
        catch
        {
            return "CronoAula";
        }
    }
}
