using System.Text.Json;
using CronoAula.Core;
using CronoAula.ViewModels;

namespace CronoAula.Tests;

/// <summary>
/// Le casos-compartilhados.json, o mesmo arquivo que os testes da versao web
/// leem. Se a regra mudar so no C# ou so no JavaScript, um dos lados falha.
/// </summary>
public class CasosCompartilhadosTests
{
    private static JsonElement Casos()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var candidato = Path.Combine(dir, "casos-compartilhados.json");
            if (File.Exists(candidato))
                return JsonDocument.Parse(File.ReadAllText(candidato)).RootElement;
            dir = Path.GetDirectoryName(dir);
        }

        throw new FileNotFoundException("casos-compartilhados.json nao foi encontrado a partir de " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Interpretar_SegueOsCasosCompartilhados()
    {
        var falhas = new List<string>();

        foreach (var caso in Casos().GetProperty("interpretar").EnumerateArray())
        {
            var texto = caso.GetProperty("texto").GetString();
            var esperado = caso.GetProperty("ms");
            var ok = TimeParser.TryParse(texto, out var tempo);

            if (esperado.ValueKind == JsonValueKind.Null)
            {
                if (ok)
                    falhas.Add($"\"{texto}\" deveria ser rejeitado");
            }
            else if (!ok || tempo.TotalMilliseconds != esperado.GetDouble())
            {
                var obtido = ok ? tempo.TotalMilliseconds.ToString() : "rejeitado";
                falhas.Add($"\"{texto}\": esperado {esperado.GetDouble()} ms, obtido {obtido}");
            }
        }

        Assert.True(falhas.Count == 0, string.Join("; ", falhas));
    }

    [Fact]
    public void Formatar_SegueOsCasosCompartilhados()
    {
        var falhas = new List<string>();

        foreach (var caso in Casos().GetProperty("formatar").EnumerateArray())
        {
            var ms = caso.GetProperty("ms").GetDouble();
            var esperado = caso.GetProperty("texto").GetString();
            var obtido = TimeParser.Format(TimeSpan.FromMilliseconds(ms));

            if (obtido != esperado)
                falhas.Add($"{ms} ms: esperado \"{esperado}\", obtido \"{obtido}\"");
        }

        Assert.True(falhas.Count == 0, string.Join("; ", falhas));
    }

    [Fact]
    public void Faixa_SegueOsCasosCompartilhados()
    {
        var falhas = new List<string>();

        foreach (var caso in Casos().GetProperty("faixa").EnumerateArray())
        {
            var restante = TimeSpan.FromMilliseconds(caso.GetProperty("restanteMs").GetDouble());
            var duracao = TimeSpan.FromMilliseconds(caso.GetProperty("duracaoMs").GetDouble());
            var esperado = caso.GetProperty("faixa").GetString();
            var obtido = MainViewModel.ComputeAlert(restante, duracao).ToString().ToLowerInvariant();

            if (obtido != esperado)
                falhas.Add($"{restante.TotalMilliseconds} de {duracao.TotalMilliseconds} ms: esperado {esperado}, obtido {obtido}");
        }

        Assert.True(falhas.Count == 0, string.Join("; ", falhas));
    }
}
