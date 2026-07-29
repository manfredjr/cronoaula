using System.Globalization;
using System.Windows;
using CronoAula.Core;

namespace CronoAula;

/// <summary>
/// Janela de preferencias. Trabalha sobre uma copia dos valores e so grava no
/// objeto real quando o usuario confirma, para que "Cancelar" nao deixe rastro.
/// </summary>
public partial class PreferencesWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SoundService _sound = new();

    public PreferencesWindow(AppSettings settings)
    {
        InitializeComponent();
        TemaJanela.UsarBarraEscura(this);
        _settings = settings;

        SliderVolume.ValueChanged += (_, _) =>
            LblVolume.Text = $"{SliderVolume.Value * 100:F0}%";

        Preencher();
    }

    private void Preencher()
    {
        ChkSom.IsChecked = _settings.SoundEnabled;
        SliderVolume.Value = _settings.Volume;
        LblVolume.Text = $"{_settings.Volume * 100:F0}%";
        CampoRepeticoes.Text = _settings.AlertRepetitions.ToString(CultureInfo.InvariantCulture);
        CampoIntervalo.Text = _settings.AlertIntervalSeconds.ToString("0.#", CultureInfo.InvariantCulture);

        ChkNegativo.IsChecked = _settings.AllowOvertime;
        ChkAvisoAntecipado.IsChecked = _settings.EarlyWarningEnabled;
        CampoAvisoMinutos.Text = _settings.EarlyWarningMinutes.ToString(CultureInfo.InvariantCulture);

        CampoPresets.Text = string.Join(", ", _settings.Presets);

        CampoAtalhoIniciar.Text = Atalho(HotkeyAction.IniciarPausar);
        CampoAtalhoZerar.Text = Atalho(HotkeyAction.Zerar);
        CampoAtalhoMais.Text = Atalho(HotkeyAction.AdicionarMinuto);
        CampoAtalhoMenos.Text = Atalho(HotkeyAction.SubtrairMinuto);
        CampoAtalhoMostrar.Text = Atalho(HotkeyAction.MostrarEsconder);
        CampoAtalhoTelaCheia.Text = Atalho(HotkeyAction.TelaCheia);
    }

    private string Atalho(HotkeyAction acao) =>
        _settings.Hotkeys.TryGetValue(acao.ToString(), out var v) ? v : "";

    private void TestarAlerta_Click(object sender, RoutedEventArgs e)
    {
        // Testa exatamente o que o professor vai ouvir, inclusive a repeticao.
        var repeticoes = int.TryParse(CampoRepeticoes.Text.Trim(), out var r) ? r : 5;
        var intervalo = double.TryParse(CampoIntervalo.Text.Trim().Replace(',', '.'),
            NumberStyles.Float, CultureInfo.InvariantCulture, out var iv) ? iv : 3.0;

        _sound.StartAlert(ChkSom.IsChecked == true, SliderVolume.Value, repeticoes, intervalo);
    }

    private void TestarAviso_Click(object sender, RoutedEventArgs e)
    {
        _sound.PlayWarning(ChkSom.IsChecked == true, SliderVolume.Value);
    }

    private void PararSom_Click(object sender, RoutedEventArgs e) => _sound.StopAlert();

    private void Restaurar_Click(object sender, RoutedEventArgs e)
    {
        var padrao = new AppSettings();

        ChkSom.IsChecked = padrao.SoundEnabled;
        SliderVolume.Value = padrao.Volume;
        CampoRepeticoes.Text = padrao.AlertRepetitions.ToString(CultureInfo.InvariantCulture);
        CampoIntervalo.Text = padrao.AlertIntervalSeconds.ToString("0.#", CultureInfo.InvariantCulture);
        ChkNegativo.IsChecked = padrao.AllowOvertime;
        ChkAvisoAntecipado.IsChecked = padrao.EarlyWarningEnabled;
        CampoAvisoMinutos.Text = padrao.EarlyWarningMinutes.ToString(CultureInfo.InvariantCulture);
        CampoPresets.Text = string.Join(", ", padrao.Presets);

        CampoAtalhoIniciar.Text = padrao.Hotkeys[nameof(HotkeyAction.IniciarPausar)];
        CampoAtalhoZerar.Text = padrao.Hotkeys[nameof(HotkeyAction.Zerar)];
        CampoAtalhoMais.Text = padrao.Hotkeys[nameof(HotkeyAction.AdicionarMinuto)];
        CampoAtalhoMenos.Text = padrao.Hotkeys[nameof(HotkeyAction.SubtrairMinuto)];
        CampoAtalhoMostrar.Text = padrao.Hotkeys[nameof(HotkeyAction.MostrarEsconder)];
        CampoAtalhoTelaCheia.Text = padrao.Hotkeys[nameof(HotkeyAction.TelaCheia)];
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        _sound.StopAlert(); // interrompe qualquer teste em andamento
        DialogResult = false;
        Close();
    }

    private void Salvar_Click(object sender, RoutedEventArgs e)
    {
        // --- Validacao antes de gravar qualquer coisa ---

        if (!int.TryParse(CampoRepeticoes.Text.Trim(), out var repeticoes) || repeticoes is < 0 or > 60)
        {
            Avisar("Escreva um número de 0 a 60 para as repetições do alerta.\n\n"
                   + "Com 0, o alerta toca até você mexer no cronômetro.");
            CampoRepeticoes.Focus();
            return;
        }

        if (!double.TryParse(CampoIntervalo.Text.Trim().Replace(',', '.'),
                NumberStyles.Float, CultureInfo.InvariantCulture, out var intervalo)
            || intervalo is < 1 or > 30)
        {
            Avisar("O intervalo entre as repetições vai de 1 a 30 segundos.");
            CampoIntervalo.Focus();
            return;
        }

        if (!int.TryParse(CampoAvisoMinutos.Text.Trim(), out var avisoMin) || avisoMin is < 1 or > 120)
        {
            Avisar("O aviso antecipado vai de 1 a 120 minutos, em número inteiro.");
            CampoAvisoMinutos.Focus();
            return;
        }

        var presets = new List<int>();
        foreach (var parte in CampoPresets.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(parte, out var m) || m is < 1 or > 600)
            {
                Avisar($"Não entendi \"{parte}\". Escreva minutos inteiros, de 1 a 600.");
                CampoPresets.Focus();
                return;
            }
            if (!presets.Contains(m))
                presets.Add(m);
        }

        if (presets.Count == 0)
        {
            Avisar("Informe ao menos um tempo rápido.");
            CampoPresets.Focus();
            return;
        }

        if (presets.Count > 6)
        {
            Avisar("Cabem no máximo 6 tempos rápidos. Acima disso a janela fica larga demais.");
            CampoPresets.Focus();
            return;
        }

        var atalhos = new Dictionary<string, string>
        {
            [nameof(HotkeyAction.IniciarPausar)] = CampoAtalhoIniciar.Text.Trim(),
            [nameof(HotkeyAction.Zerar)] = CampoAtalhoZerar.Text.Trim(),
            [nameof(HotkeyAction.AdicionarMinuto)] = CampoAtalhoMais.Text.Trim(),
            [nameof(HotkeyAction.SubtrairMinuto)] = CampoAtalhoMenos.Text.Trim(),
            [nameof(HotkeyAction.MostrarEsconder)] = CampoAtalhoMostrar.Text.Trim(),
            [nameof(HotkeyAction.TelaCheia)] = CampoAtalhoTelaCheia.Text.Trim()
        };

        // Campo vazio significa "atalho desativado"; o resto precisa ser valido.
        foreach (var (acao, combo) in atalhos)
        {
            if (string.IsNullOrWhiteSpace(combo))
                continue;

            if (!HotkeyCombo.TryParse(combo, out _, out _))
            {
                Avisar($"Não reconheci o atalho \"{combo}\".\n\n"
                       + "Comece por Ctrl, Alt, Shift ou Win e termine com uma tecla, "
                       + "como em Ctrl+Alt+S, Ctrl+Shift+F9 ou Ctrl+Alt+Up.");
                return;
            }
        }

        // --- Tudo validado: agora sim grava ---

        _sound.StopAlert(); // um teste em andamento nao deve continuar tocando

        _settings.SoundEnabled = ChkSom.IsChecked == true;
        _settings.Volume = Math.Clamp(SliderVolume.Value, 0.0, 1.0);
        _settings.AlertRepetitions = repeticoes;
        _settings.AlertIntervalSeconds = intervalo;
        _settings.AllowOvertime = ChkNegativo.IsChecked == true;
        _settings.EarlyWarningEnabled = ChkAvisoAntecipado.IsChecked == true;
        _settings.EarlyWarningMinutes = avisoMin;
        _settings.Presets = presets;
        _settings.Hotkeys = atalhos;

        DialogResult = true;
        Close();
    }

    private void Avisar(string mensagem) =>
        MessageBox.Show(this, mensagem, "CronoAula - preferências",
            MessageBoxButton.OK, MessageBoxImage.Warning);
}
