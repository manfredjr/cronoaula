using System.Windows;
using CronoAula.Core;

namespace CronoAula;

/// <summary>
/// Janela "Sobre": identifica o programa e registra por que ele foi criado.
///
/// A versao e o aviso de direitos autorais vem de <see cref="AppInfo"/>, que le
/// os metadados do assembly de um jeito que funciona tambem no executavel unico.
/// Nao volte a usar Assembly.Location aqui: veja a explicacao em AppInfo.cs.
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        TemaJanela.UsarBarraEscura(this);

        LblVersao.Text = $"Versão {AppInfo.Versao}";

        var copyright = AppInfo.Copyright;
        if (!string.IsNullOrWhiteSpace(copyright))
            LblCopyright.Text = copyright;
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();
}
