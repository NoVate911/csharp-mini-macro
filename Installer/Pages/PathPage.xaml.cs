using MiniMacroInstaller.Helpers;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace MiniMacroInstaller.Pages
{
    public partial class PathPage : UserControl
    {
        public string InstallPath => PathBox.Text;
        public bool CreateDesktopShortcut => DesktopShortcut.IsChecked == true;
        public bool CreateStartMenuShortcut => StartMenuShortcut.IsChecked == true;

        public PathPage(bool isModern)
        {
            InitializeComponent();
            SetVersionInfo(isModern);
            UpdateSpaceInfo();
        }

        private void SetVersionInfo(bool isModern)
        {
            if (isModern)
            {
                VersionInfoTitle.Text = "Версия Modern (.NET 10.0)";
                VersionInfoText.Text = "Требует .NET Desktop Runtime 10.0. Если не установлен — будет загружен автоматически.";
            }
            else
            {
                VersionInfoTitle.Text = "Версия Legacy (.NET Framework 4.8)";
                VersionInfoText.Text = "Требует .NET Framework 4.8. Поставляется вместе с Windows 10 (1903+) или устанавливается отдельно.";
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Выберите папку для установки Mini Macro",
                SelectedPath = PathBox.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PathBox.Text = dialog.SelectedPath;
                UpdateSpaceInfo();
            }
        }

        private void UpdateSpaceInfo()
        {
            try
            {
                var drive = Path.GetPathRoot(PathBox.Text);
                if (drive == null) return;
                var info = new DriveInfo(drive);
                var freeMb = info.AvailableFreeSpace / 1024 / 1024;
                SpaceText.Text = $"Доступно на диске: {freeMb:N0} МБ";
            }
            catch
            {
                SpaceText.Text = string.Empty;
            }
        }
    }
}
