using System.IO;
using System.Windows;
using System.Windows.Threading;
using CronoAula.Core;

namespace CronoAula;

public partial class App : Application
{
    /// <summary>
    /// Rede de protecao contra erros nao tratados.
    ///
    /// Motivo concreto: na versao 1.0.0, uma excecao ao abrir a janela "Sobre"
    /// derrubava o programa inteiro. Em sala de aula isso e grave - significa
    /// perder a contagem no meio de uma prova por causa de um detalhe cosmetico.
    ///
    /// Agora um erro em uma janela secundaria e registrado, informado ao usuario
    /// e o cronometro continua rodando.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // "CronoAula.exe --sobre" abre direto a janela de identificacao do
        // programa. Serve para conferir versao e autoria sem precisar abrir o
        // menu, e permite testar essa janela no executavel publicado.
        if (e.Args.Any(a => a.Equals("--sobre", StringComparison.OrdinalIgnoreCase)
                            || a.Equals("/sobre", StringComparison.OrdinalIgnoreCase)))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            new AboutWindow().ShowDialog();
            Shutdown();
            return;
        }

        // Erros na thread da interface: tratados sem encerrar o processo.
        DispatcherUnhandledException += (_, args) =>
        {
            RegistrarErro(args.Exception);

            MessageBox.Show(
                "Aconteceu um erro inesperado. O cronômetro continua rodando e a "
                + "contagem não se perdeu.\n\n"
                + $"Erro: {args.Exception.Message}\n\n"
                + $"O registro ficou salvo em:\n{CaminhoDoLog}",
                "CronoAula",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            args.Handled = true; // impede o encerramento do processo
        };

        // Erros em outras threads (ex.: o timer do alerta sonoro) nao podem ser
        // impedidos de encerrar o processo, mas ao menos ficam registrados.
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                RegistrarErro(ex);
        };

        // A janela principal e criada aqui, e nao por StartupUri no App.xaml,
        // para que o modo "--sobre" acima possa evitar abri-la.
        new MainWindow().Show();
    }

    private static string CaminhoDoLog =>
        Path.Combine(AppSettings.ConfigDirectory, "erros.log");

    /// <summary>
    /// Anexa o erro a %APPDATA%\CronoAula\erros.log. O registro so ajuda se o
    /// proprio registro nao quebrar, entao qualquer falha aqui e ignorada.
    /// </summary>
    private static void RegistrarErro(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(AppSettings.ConfigDirectory);

            var texto =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CronoAula {AppInfo.Versao}"
                + Environment.NewLine
                + ex
                + Environment.NewLine
                + new string('-', 70)
                + Environment.NewLine;

            File.AppendAllText(CaminhoDoLog, texto);
        }
        catch
        {
            // Sem disco, sem permissao: nada a fazer, e nao vale derrubar o app.
        }
    }
}
