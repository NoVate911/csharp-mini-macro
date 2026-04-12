using MiniMacroInstaller.Helpers;
using System.Windows.Controls;

namespace MiniMacroInstaller.Pages
{
    public partial class WelcomePage : UserControl
    {
        public bool IsModernSelected => ModernCard.IsChecked == true;

        public WelcomePage()
        {
            InitializeComponent();

            OsText.Text = OsHelper.GetOsDisplayName();

            // Авто-выбор версии по ОС
            if (OsHelper.IsWindows10OrNewer())
            {
                ModernCard.IsChecked = true;
            }
            else
            {
                LegacyCard.IsChecked = true;
                ShowOsWarning(isModernOnOld: false);
            }
        }

        private void ModernCard_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            LegacyCard.IsChecked = false;
            if (!OsHelper.IsWindows10OrNewer())
                ShowOsWarning(isModernOnOld: true);
            else
                OsWarnBorder.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void LegacyCard_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            ModernCard.IsChecked = false;
            OsWarnBorder.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void Card_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            // Не даём снять оба флага — хотя бы один должен быть выбран
            if (ModernCard.IsChecked != true && LegacyCard.IsChecked != true)
                ((System.Windows.Controls.Primitives.ToggleButton)sender).IsChecked = true;
        }

        private void ShowOsWarning(bool isModernOnOld)
        {
            OsWarnBorder.Visibility = System.Windows.Visibility.Visible;
            OsWarnText.Text = isModernOnOld
                ? "Версия Modern требует Windows 10 или новее. На вашей системе она может не запуститься."
                : "Обнаружена Windows 7. Рекомендуется версия Legacy (.NET Framework 4.8).";
        }
    }
}
