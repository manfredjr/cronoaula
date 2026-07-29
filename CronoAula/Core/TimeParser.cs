using System.Globalization;

namespace CronoAula.Core;

/// <summary>
/// Converte texto digitado pelo usuario em <see cref="TimeSpan"/> e formata o tempo
/// para exibicao. Aceita os formatos combinados no README:
///   - "MM:SS"   -> minutos e segundos      (ex.: "25:30")
///   - "HH:MM:SS"-> horas, minutos, segundos (ex.: "01:05:00")
///   - "25"      -> apenas minutos            (ex.: 25 minutos)
///   - "25.5"    -> minutos com fracao        (30 s = 0,5 min)
/// </summary>
public static class TimeParser
{
    /// <summary>
    /// Tenta interpretar o texto. Retorna true e preenche <paramref name="result"/>
    /// em caso de sucesso. Nunca lanca excecao.
    /// </summary>
    public static bool TryParse(string? text, out TimeSpan result)
    {
        result = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();

        if (text.Contains(':'))
        {
            var parts = text.Split(':');
            if (parts.Length is < 2 or > 3)
                return false;

            // Cada componente deve ser um inteiro nao negativo.
            var numbers = new int[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
                    return false;
            }

            if (parts.Length == 2)
            {
                // MM:SS
                var (mm, ss) = (numbers[0], numbers[1]);
                if (ss > 59)
                    return false;
                result = new TimeSpan(0, mm, ss);
            }
            else
            {
                // HH:MM:SS
                var (hh, mm, ss) = (numbers[0], numbers[1], numbers[2]);
                if (mm > 59 || ss > 59)
                    return false;
                result = new TimeSpan(hh, mm, ss);
            }

            return true;
        }

        // Sem ":" => apenas minutos (aceita fracao com ponto ou virgula).
        var normalized = text.Replace(',', '.');
        if (double.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var minutes)
            && minutes >= 0)
        {
            result = TimeSpan.FromMinutes(minutes);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Formata para exibicao. Usa "MM:SS" por padrao e "HH:MM:SS" a partir de 1 hora.
    /// Tempo negativo (excedido) recebe o prefixo "-", ex.: "-01:20".
    /// </summary>
    public static string Format(TimeSpan time)
    {
        var negative = time < TimeSpan.Zero;
        var abs = negative ? time.Negate() : time;

        // Arredonda para o segundo mais proximo para o display nao "tremer".
        var totalSeconds = (long)Math.Round(abs.TotalSeconds, MidpointRounding.AwayFromZero);
        var hh = totalSeconds / 3600;
        var mm = (totalSeconds % 3600) / 60;
        var ss = totalSeconds % 60;

        var body = hh >= 1
            ? $"{hh:00}:{mm:00}:{ss:00}"
            : $"{mm:00}:{ss:00}";

        return negative ? "-" + body : body;
    }
}
