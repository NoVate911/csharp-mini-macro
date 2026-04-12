using MiniMacroInstaller.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MiniMacroInstaller.Pages
{
    public partial class InstallPage : UserControl
    {
        public bool IsCompleted { get; private set; }
        public bool HadError     { get; private set; }
        public string InstallDir { get; private set; } = string.Empty;

        public InstallPage() => InitializeComponent();

        public async Task RunAsync(bool isModern, string installDir,
                                   bool desktopShortcut, bool startMenuShortcut)
        {
            InstallDir = installDir;
            HadError = false;

            try
            {
                await Task.Run(() =>
                {
                    // 1. Проверить и установить runtime
                    SetProgress(10, "Проверка зависимостей...");

                    if (isModern && !RuntimeHelper.IsNet10Installed())
                    {
                        SetProgress(20, "Установка .NET Desktop Runtime 10...");
                        RuntimeHelper.InstallNet10(msg => AppendLog(msg));
                    }
                    else if (!isModern && !RuntimeHelper.IsNetFx48Installed())
                    {
                        SetProgress(20, "Установка .NET Framework 4.8...");
                        RuntimeHelper.InstallNetFx48(msg => AppendLog(msg));
                    }
                    else
                    {
                        AppendLog("✓ Зависимости уже установлены.");
                    }

                    // 2. Установить файлы
                    SetProgress(55, "Копирование файлов...");
                    InstallHelper.Install(
                        installDir, isModern,
                        desktopShortcut, startMenuShortcut,
                        msg =>
                        {
                            AppendLog(msg);
                        });

                    SetProgress(100, "Установка завершена");
                });

                IsCompleted = true;
            }
            catch (Exception ex)
            {
                HadError = true;
                AppendLog($"✗ Ошибка: {ex.Message}");
                SetProgress(0, "Ошибка при установке");
            }
        }

        private void SetProgress(int percent, string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = status;
                PercentText.Text = $"{percent}%";

                // Ширина полосы прогресса пропорциональна ширине контейнера
                var container = ProgressFill.Parent as Border;
                if (container != null)
                {
                    double totalWidth = container.ActualWidth;
                    ProgressFill.Width = Math.Max(0, totalWidth * percent / 100.0);
                }
            });
        }

        private void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogText.Text += message + Environment.NewLine;
                LogScroll.ScrollToEnd();
            });
        }
    }
}
