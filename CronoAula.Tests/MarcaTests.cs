using System.Globalization;
using System.Text.RegularExpressions;

namespace CronoAula.Tests;

/// <summary>
/// Protege a identidade visual MT - Manfred Tecnologia.
///
/// O manual da marca exige contraste minimo de 4,5 para texto normal e 3,0 para
/// texto grande. Estes testes recalculam os valores a partir das cores que estao
/// de fato no dicionario Marca.xaml, entao qualquer troca de cor que quebre a
/// legibilidade falha aqui antes de chegar na sala de aula.
/// </summary>
public class MarcaTests
{
    // Paleta oficial, conforme identidade_visual_manfred_tecnologia.md
    private const string VerdeClaro = "6AAF21";
    private const string VerdeMedio = "398024";
    private const string VerdeEscuro = "04511F";
    private const string VerdeProfundo = "022F10";
    private const string Areia = "F5F7F2";
    private const string Branco = "FFFFFF";

    // Escala de alerta, clareada para funcionar sobre fundo escuro.
    private const string AlertaAtencao = "E09B2D";
    private const string AlertaUrgente = "E8752D";
    private const string AlertaEstourado = "E86A6F";

    private const string TextoSecundario = "98A199";
    // Fundo real das janelas: grafite, o neutro escuro do manual.
    private const string Fundo = "1E2422";
    private const string BotaoSecundario = "333B37";

    // --- Calculo de contraste conforme WCAG 2.1 ---

    private static double Luminancia(string hex)
    {
        double Canal(int inicio)
        {
            var v = int.Parse(hex.Substring(inicio, 2), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture) / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Canal(0) + 0.7152 * Canal(2) + 0.0722 * Canal(4);
    }

    private static double Contraste(string a, string b)
    {
        var la = Luminancia(a);
        var lb = Luminancia(b);
        var (alto, baixo) = la > lb ? (la, lb) : (lb, la);
        return (alto + 0.05) / (baixo + 0.05);
    }

    [Fact]
    public void CalculoDeContraste_BateComOManual()
    {
        // Valores publicados na tabela do manual, para provar que a formula
        // usada aqui e a mesma que gerou aquele documento.
        Assert.Equal(9.54, Contraste(VerdeEscuro, Branco), 1);
        Assert.Equal(4.90, Contraste(VerdeMedio, Branco), 1);
        Assert.Equal(5.48, Contraste(VerdeClaro, VerdeProfundo), 1);
        Assert.Equal(2.70, Contraste(VerdeClaro, Branco), 1);
    }

    [Theory]
    [InlineData(Areia, "tempo normal")]
    [InlineData(AlertaAtencao, "reta final")]
    [InlineData(AlertaUrgente, "último minuto")]
    [InlineData(AlertaEstourado, "tempo excedido")]
    public void EscalaDeAlerta_LegivelSobreOFundoEscuro(string cor, string faixa)
    {
        var razao = Contraste(Fundo, cor);

        // 4,5 e o minimo para texto normal. Os digitos sao enormes, entao aqui
        // sobra folga; a exigencia rigorosa e de proposito.
        Assert.True(razao >= 4.5,
            $"A faixa \"{faixa}\" ficou em {razao:F2}, abaixo do minimo de 4,5.");
    }

    [Fact]
    public void FaixasDeAlerta_SaoCoresDistintas()
    {
        var cores = new[] { Areia, AlertaAtencao, AlertaUrgente, AlertaEstourado };

        Assert.Equal(cores.Length, cores.Distinct().Count());
    }

    [Fact]
    public void BotaoPrincipal_UsaOParAprovadoPeloManual()
    {
        // Verde claro com texto verde profundo. Texto branco daria 2,70, que o
        // manual proibe explicitamente.
        Assert.True(Contraste(VerdeClaro, VerdeProfundo) >= 4.5);
        Assert.True(Contraste(VerdeClaro, Branco) < 3.0,
            "Se este valor subir, alguem trocou o verde claro da marca.");
    }

    [Fact]
    public void BotaoSecundario_TemTextoLegivel()
    {
        Assert.True(Contraste(BotaoSecundario, Areia) >= 4.5);
    }

    [Fact]
    public void PresetArmado_TemTextoLegivel()
    {
        Assert.True(Contraste(VerdeMedio, Branco) >= 4.5);
    }

    [Fact]
    public void TextoSecundario_FoiClareadoPorqueOCinzaDoManualNaoServeAqui()
    {
        // O cinza medio do manual (#6B736E) e definido para fundo claro. Sobre o
        // grafite ele nao alcanca o minimo para texto pequeno.
        Assert.True(Contraste(Fundo, "6B736E") < 4.5);

        // A versao clareada resolve.
        Assert.True(Contraste(Fundo, TextoSecundario) >= 4.5);
    }

    [Fact]
    public void FuncionaisDoManual_NaoServemEmFundoEscuro()
    {
        // Registra o motivo de termos clareado ambar e vinho. Se algum dia
        // alguem tentar usar os valores originais no cronometro, este teste
        // documenta por que nao da.
        Assert.True(Contraste(Fundo, "9A5B00") < 3.0, "ambar original");
        Assert.True(Contraste(Fundo, "9B2226") < 3.0, "vinho original");
    }

    // --- Coerencia entre o dicionario e estes testes ---

    private static string LerMarcaXaml()
    {
        // Sobe a partir da pasta de saida dos testes ate a raiz do projeto.
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var candidato = Path.Combine(dir, "CronoAula", "Marca.xaml");
            if (File.Exists(candidato))
                return File.ReadAllText(candidato);
            dir = Path.GetDirectoryName(dir);
        }

        throw new FileNotFoundException("Marca.xaml nao foi encontrado a partir de " + AppContext.BaseDirectory);
    }

    [Theory]
    [InlineData(VerdeClaro)]
    [InlineData(VerdeMedio)]
    [InlineData(VerdeEscuro)]
    [InlineData(VerdeProfundo)]
    [InlineData(Areia)]
    [InlineData(AlertaAtencao)]
    [InlineData(AlertaUrgente)]
    [InlineData(AlertaEstourado)]
    [InlineData(TextoSecundario)]
    [InlineData(Fundo)]
    [InlineData(BotaoSecundario)]
    public void CorTestadaAquiExisteMesmoNoDicionario(string hex)
    {
        // Sem isto, os testes poderiam aprovar cores que o aplicativo nao usa.
        var xaml = LerMarcaXaml();

        Assert.Contains(hex, xaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FundoNeutro_RendeMaisQueOFundoVerde()
    {
        // Registra a decisao de trocar o fundo verde profundo pelo grafite.
        // O verde de fundo deixava a janela verde demais e ainda custava
        // contraste em todas as faixas de alerta.
        foreach (var cor in new[] { Areia, AlertaAtencao, AlertaUrgente, AlertaEstourado })
        {
            Assert.True(Contraste(Fundo, cor) > Contraste(VerdeProfundo, cor),
                $"A cor {cor} deveria render mais sobre o grafite do que sobre o verde profundo.");
        }
    }

    [Fact]
    public void BotaoPrincipal_SeDestacaDoSecundario()
    {
        // O botao principal e o alvo mais importante da janela. Se ele nao se
        // separar bem dos demais, o professor perde o Iniciar de vista.
        var separacao = Contraste(VerdeClaro, BotaoSecundario);

        Assert.True(separacao >= 3.0,
            $"Separacao de apenas {separacao:F2} entre o botao principal e os secundarios.");
    }

    [Fact]
    public void DicionarioNaoTemMaisAsCoresAntigas()
    {
        var xaml = LerMarcaXaml();

        // Azul e cinzas da versao anterior, antes da identidade da marca.
        Assert.DoesNotContain("4C6EF5", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("1C1C1E", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FFD24A", xaml, StringComparison.OrdinalIgnoreCase);
    }
}
