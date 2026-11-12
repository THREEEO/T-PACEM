using System.Windows;

namespace T_PACE
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                DatabaseConfig.InicializarBanco();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var login = new LoginWindow();
            bool? ok = login.ShowDialog();

            if (ok == true)
            {
                var main = new MainWindow();
                this.MainWindow = main;
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                main.Show();
            }
            else
            {
                Shutdown();
            }
        }
    }
}