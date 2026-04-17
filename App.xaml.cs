using System.Windows;

namespace MiniMacro
{
    public partial class App : Application
    {
        internal static MacroEngine Engine { get; } = new MacroEngine();

        protected override void OnStartup(StartupEventArgs e)
        {
            SettingsManager.Load();
            ThemeManager.Apply(SettingsManager.Current.Theme);
            base.OnStartup(e);
        }
    }
}
