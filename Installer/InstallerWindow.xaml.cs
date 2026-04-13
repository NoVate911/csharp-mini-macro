using MiniMacroInstaller.Helpers;
using MiniMacroInstaller.Pages;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MiniMacroInstaller
{
    public partial class InstallerWindow : Window
    {
        private enum Step { Welcome = 1, Path = 2, Install = 3, Finish = 4 }
        private Step _currentStep = Step.Welcome;

        private WelcomePage? _welcomePage;
        private PathPage?    _pathPage;
        private InstallPage? _installPage;
        private FinishPage?  _finishPage;

        public InstallerWindow()
        {
            InitializeComponent();
            ShowPage(Step.Welcome);
        }

        // ── Навигация ────────────────────────────────────────────────────────

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == Step.Welcome)
            {
                ShowPage(Step.Path);
            }
            else if (_currentStep == Step.Path)
            {
                // Проверить наличие assets до начала установки
                var isModern = _welcomePage!.IsModernSelected;
                if (!InstallHelper.AssetExists(isModern))
                {
                    var which = isModern ? "MiniMacro-modern.exe" : "MiniMacro-legacy.exe";
                    MessageBox.Show(
                        $"Файл {which} не найден в папке assets/\n\n" +
                        "Соберите основной проект и скопируйте exe в папку assets/ рядом с Setup.exe.",
                        "Файл не найден", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ShowPage(Step.Install);

                // Запуск установки
                NextButton.IsEnabled = false;
                BackButton.IsEnabled = false;
                CancelButton.IsEnabled = false;

                await _installPage!.RunAsync(
                    isModern,
                    _pathPage!.InstallPath,
                    _pathPage.CreateDesktopShortcut,
                    _pathPage.CreateStartMenuShortcut);

                if (_installPage.HadError)
                {
                    NextButton.IsEnabled = false;
                    BackButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;
                    MessageBox.Show("Во время установки произошла ошибка.\nПосмотрите лог выше.",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    ShowPage(Step.Welcome);
                }
                else
                {
                    ShowPage(Step.Finish);
                }
            }
            else if (_currentStep == Step.Finish)
            {
                if (_finishPage!.ShouldLaunch)
                    InstallHelper.LaunchApp(_installPage!.InstallDir);
                Close();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == Step.Path)
                ShowPage(Step.Welcome);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Прервать установку?", "Mini Macro Setup",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Close();
        }

        // ── Переключение страниц ─────────────────────────────────────────────

        private void ShowPage(Step step)
        {
            _currentStep = step;

            switch (step)
            {
                case Step.Welcome:
                    _welcomePage = new WelcomePage();
                    PageContent.Content = _welcomePage;
                    NextButtonText.Text = "Далее";
                    BackButton.IsEnabled = false;
                    NextButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;
                    break;

                case Step.Path:
                    _pathPage = new PathPage(_welcomePage!.IsModernSelected);
                    PageContent.Content = _pathPage;
                    NextButtonText.Text = "Установить";
                    BackButton.IsEnabled = true;
                    NextButton.IsEnabled = true;
                    break;

                case Step.Install:
                    _installPage = new InstallPage();
                    PageContent.Content = _installPage;
                    NextButtonText.Text = "Подождите...";
                    BackButton.IsEnabled = false;
                    NextButton.IsEnabled = false;
                    CancelButton.IsEnabled = false;
                    break;

                case Step.Finish:
                    _finishPage = new FinishPage(
                        _installPage!.InstallDir,
                        _welcomePage!.IsModernSelected);
                    PageContent.Content = _finishPage;
                    NextButtonText.Text = "Готово";
                    BackButton.IsEnabled = false;
                    NextButton.IsEnabled = true;
                    CancelButton.IsEnabled = false;
                    break;
            }

            UpdateStepIndicator(step);
        }

        private void UpdateStepIndicator(Step step)
        {
            var accentColor   = (SolidColorBrush)FindResource("AccentBrush");
            var dividerColor  = (SolidColorBrush)FindResource("DividerBrush");
            var primaryText   = (SolidColorBrush)FindResource("TextPrimaryBrush");
            var secondaryText = (SolidColorBrush)FindResource("TextSecondaryBrush");
            var darkText      = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14));

            void SetStep(int s, System.Windows.Shapes.Ellipse dot,
                         System.Windows.Controls.TextBlock num,
                         System.Windows.Controls.TextBlock label)
            {
                bool active = (int)step == s;
                bool done   = (int)step > s;

                dot.Fill       = (active || done) ? accentColor : dividerColor;
                num.Foreground = (active || done) ? darkText    : secondaryText;
                label.Foreground = active ? accentColor
                                 : done   ? primaryText
                                          : secondaryText;
            }

            SetStep(1, Step1Dot, Step1Num, Step1Text);
            SetStep(2, Step2Dot, Step2Num, Step2Text);
            SetStep(3, Step3Dot, Step3Num, Step3Text);
            SetStep(4, Step4Dot, Step4Num, Step4Text);
        }

        // ── Хром окна ────────────────────────────────────────────────────────

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Cancel_Click(sender, e);
        }
    }
}
