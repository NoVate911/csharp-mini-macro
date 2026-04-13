using MiniMacroInstaller.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MiniMacroInstaller
{
    public partial class UninstallWindow : Window
    {
        private string _installDir = string.Empty;

        public UninstallWindow()
        {
            InitializeComponent();
            LoadInstallInfo();
        }

        private void LoadInstallInfo()
        {
            _installDir = UninstallHelper.GetInstallDir() ?? string.Empty;

            if (string.IsNullOrEmpty(_installDir))
            {
                InstallDirText.Text = "Папка установки не найдена";
                UninstallButton.IsEnabled = false;
            }
            else
            {
                InstallDirText.Text = _installDir;
            }
        }

        private async void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            // Переключаемся в режим прогресса
            ConfirmPanel.Visibility  = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            CancelButton.IsEnabled   = false;
            UninstallButton.IsEnabled = false;
            UninstallButtonText.Text = "Удаление...";

            try
            {
                await Task.Run(() =>
                {
                    UninstallHelper.Uninstall(_installDir, msg =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LogText.Text += msg + Environment.NewLine;
                            LogScroll.ScrollToEnd();
                        });
                    });
                });

                // Анимируем прогресс-бар до 100%
                AnimateProgress();

                // Запланировать самоудаление exe и папки
                UninstallHelper.ScheduleSelfDelete(_installDir);

                UninstallButtonText.Text  = "Готово";
                UninstallButton.IsEnabled = true;
                UninstallButton.Click    -= Uninstall_Click;
                UninstallButton.Click    += (s, ev) => Close();
            }
            catch (Exception ex)
            {
                LogText.Text += $"{Environment.NewLine}✗ Ошибка: {ex.Message}";
                CancelButton.IsEnabled    = true;
                UninstallButtonText.Text  = "Повторить";
                UninstallButton.IsEnabled = true;
            }
        }

        private void AnimateProgress()
        {
            Dispatcher.Invoke(() =>
            {
                var container = ProgressBar.Parent as System.Windows.Controls.Border;
                if (container != null)
                    ProgressBar.Width = container.ActualWidth;
            });
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
