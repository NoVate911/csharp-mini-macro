using System.Windows;

namespace MiniMacro
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            SettingsManager.Load();
            base.OnStartup(e);
        }
    }
}
