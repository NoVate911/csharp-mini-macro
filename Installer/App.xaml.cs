using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace MiniMacroInstaller
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Показываем UninstallWindow если:
            // - передан аргумент --uninstall (запуск из «Программ и компонентов»)
            // - либо сам exe называется uninstall.exe (двойной клик)
            var exeName = Path.GetFileNameWithoutExtension(
                Assembly.GetEntryAssembly()?.Location ?? string.Empty);

            bool isUninstall = (e.Args.Length > 0 && e.Args[0] == "--uninstall")
                            || exeName.Equals("uninstall", StringComparison.OrdinalIgnoreCase);

            if (isUninstall)
                new UninstallWindow().Show();
            else
                new InstallerWindow().Show();
        }
    }
}
