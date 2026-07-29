using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CronoAula.Core;

/// <summary>Os tres tamanhos de exibicao previstos.</summary>
public enum DisplaySize
{
    Pequeno,
    Medio,
    Grande
}

/// <summary>Acoes que podem ser disparadas por atalho global.</summary>
public enum HotkeyAction
{
    IniciarPausar,
    Zerar,
    AdicionarMinuto,
    SubtrairMinuto,
    MostrarEsconder,
    TelaCheia
}

/// <summary>
/// Preferencias do usuario, persistidas em %APPDATA%\CronoAula\config.json.
///
/// Regra importante: o aplicativo precisa iniciar normalmente mesmo se o arquivo
/// estiver ausente ou corrompido. Por isso <see cref="Load"/> nunca lanca excecao,
/// caindo nos valores padrao definidos aqui.
/// </summary>
public sealed class AppSettings
{
    // --- Janela ---
    /// <summary>Posicao salva. Null na primeira execucao (a janela se posiciona sozinha).</summary>
    public double? Left { get; set; }
    public double? Top { get; set; }

    public DisplaySize Size { get; set; } = DisplaySize.Medio;

    /// <summary>Opacidade de 0,30 a 1,00.</summary>
    public double Opacity { get; set; } = 0.92;

    public bool AlwaysOnTop { get; set; } = true;

    public bool ShowInTaskbar { get; set; } = false;

    // --- Som ---
    public bool SoundEnabled { get; set; } = true;

    /// <summary>Volume de 0,0 a 1,0.</summary>
    public double Volume { get; set; } = 0.7;

    /// <summary>
    /// Quantas vezes o alerta de fim se repete. 0 significa repetir ate alguem
    /// mexer no cronometro (com teto de seguranca no proprio SoundService).
    /// </summary>
    public int AlertRepetitions { get; set; } = 5;

    /// <summary>Segundos entre o inicio de uma repeticao do alerta e a seguinte.</summary>
    public double AlertIntervalSeconds { get; set; } = 3.0;

    // --- Comportamento do cronometro ---
    /// <summary>Quando true, continua contando em negativo apos o zero.</summary>
    public bool AllowOvertime { get; set; } = true;

    public bool EarlyWarningEnabled { get; set; } = true;

    /// <summary>Minutos restantes em que o aviso antecipado dispara.</summary>
    public int EarlyWarningMinutes { get; set; } = 5;

    /// <summary>Ultimo tempo usado, em minutos, restaurado na proxima execucao.</summary>
    public double LastMinutes { get; set; } = 50;

    /// <summary>Botoes de tempo rapido, em minutos. Editavel nas preferencias.</summary>
    public List<int> Presets { get; set; } = new() { 5, 10, 15, 30, 50 };

    // --- Atalhos globais (texto no formato "Ctrl+Alt+S") ---
    public Dictionary<string, string> Hotkeys { get; set; } = new()
    {
        [nameof(HotkeyAction.IniciarPausar)] = "Ctrl+Alt+S",
        [nameof(HotkeyAction.Zerar)] = "Ctrl+Alt+R",
        [nameof(HotkeyAction.AdicionarMinuto)] = "Ctrl+Alt+Up",
        [nameof(HotkeyAction.SubtrairMinuto)] = "Ctrl+Alt+Down",
        [nameof(HotkeyAction.MostrarEsconder)] = "Ctrl+Alt+H",
        [nameof(HotkeyAction.TelaCheia)] = "Ctrl+Alt+F"
    };

    /// <summary>
    /// Monitor onde a tela cheia abre, guardado pelo nome do dispositivo.
    /// Null significa "usar o monitor onde a janela estiver".
    /// </summary>
    public string? FullscreenMonitor { get; set; }

    // ------------------------------------------------------------------
    // Persistencia
    // ------------------------------------------------------------------

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Pasta de configuracao: %APPDATA%\CronoAula\</summary>
    public static string ConfigDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CronoAula");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "config.json");

    /// <summary>
    /// Carrega as preferencias. Se o arquivo nao existir, estiver corrompido ou
    /// inacessivel, devolve os valores padrao em vez de falhar.
    /// </summary>
    public static AppSettings Load() => LoadFrom(ConfigPath);

    /// <summary>
    /// Mesma logica de <see cref="Load"/>, com o caminho explicito.
    /// Existe para que os testes nao precisem tocar no %APPDATA% real do usuario.
    /// </summary>
    internal static AppSettings LoadFrom(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new AppSettings();

            var json = File.ReadAllText(path);
            var loaded = JsonDeserializeSafe(json);
            return loaded is null ? new AppSettings() : loaded.Sanitized();
        }
        catch
        {
            // Arquivo ilegivel, permissao negada, disco com problema: cai no padrao.
            return new AppSettings();
        }
    }

    private static AppSettings? JsonDeserializeSafe(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
        }
        catch (JsonException)
        {
            // JSON malformado (arquivo corrompido).
            return null;
        }
    }

    /// <summary>
    /// Grava as preferencias. Escreve primeiro em arquivo temporario e so entao
    /// substitui o definitivo, para que uma queda de energia no meio da gravacao
    /// nao deixe um config.json truncado.
    /// </summary>
    public void Save() => SaveTo(ConfigPath);

    /// <summary>Mesma logica de <see cref="Save"/>, com o caminho explicito.</summary>
    internal void SaveTo(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, JsonOptions);

            var temp = path + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            // Nao conseguir salvar preferencias nunca deve derrubar o aplicativo.
        }
    }

    /// <summary>
    /// Corrige valores fora de faixa vindos de um arquivo editado a mao.
    /// </summary>
    private AppSettings Sanitized()
    {
        Opacity = Math.Clamp(double.IsFinite(Opacity) ? Opacity : 0.92, 0.30, 1.00);
        Volume = Math.Clamp(double.IsFinite(Volume) ? Volume : 0.7, 0.0, 1.0);
        EarlyWarningMinutes = Math.Clamp(EarlyWarningMinutes, 1, 120);

        // 0 e valido: significa "repetir ate alguem interromper".
        AlertRepetitions = Math.Clamp(AlertRepetitions, 0, 60);
        AlertIntervalSeconds = Math.Clamp(
            double.IsFinite(AlertIntervalSeconds) ? AlertIntervalSeconds : 3.0, 1.0, 30.0);

        if (!double.IsFinite(LastMinutes) || LastMinutes < 0 || LastMinutes > 24 * 60)
            LastMinutes = 50;

        if (Presets is null || Presets.Count == 0)
            Presets = new List<int> { 5, 10, 15, 30, 50 };
        else
            Presets = Presets.Where(p => p is > 0 and <= 600).Distinct().Take(6).ToList();

        if (Presets.Count == 0)
            Presets = new List<int> { 5, 10, 15, 30, 50 };

        // Garante que toda acao tenha um atalho definido (mesmo que vazio).
        var defaults = new AppSettings().Hotkeys;
        Hotkeys ??= new Dictionary<string, string>();
        foreach (var kv in defaults)
            if (!Hotkeys.ContainsKey(kv.Key))
                Hotkeys[kv.Key] = kv.Value;

        return this;
    }
}
