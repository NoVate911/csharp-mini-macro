using System.Windows;

namespace MiniMacroInstaller
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length > 0 && e.Args[0] == "--uninstall")
                new UninstallWindow().Show();
            else
                new InstallerWindow().Show();
        }
    }
}
