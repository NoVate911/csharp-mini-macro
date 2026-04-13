using System.Windows.Controls;

namespace MiniMacroInstaller.Pages
{
    public partial class FinishPage : UserControl
    {
        public bool ShouldLaunch => LaunchAppCheck.IsChecked == true;

        public FinishPage(string installDir, bool isModern)
        {
            InitializeComponent();
            SubtitleText.Text =
                $"Mini Macro ({(isModern ? "Modern" : "Legacy")}) установлен в\n{installDir}";
        }
    }
}
