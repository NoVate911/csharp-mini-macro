using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MiniMacro
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _debugTimer;

        public MainWindow()
        {
            InitializeComponent();

            // Когда пользователь загружает макрос из библиотеки
            MacroLibrary.MacroLoaded += OnMacroLoaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HotkeyManager.Initialize(this);
            HotkeyManager.HotkeyFired      += OnHotkeyFired;
            HotkeyManager.MacroHotkeyFired += OnMacroHotkeyFired;
            HotkeyManager.RegisterAll();

            // Зарегистрировать хоткеи макросов из сохранённых настроек
            var macros = MacroLibrary.LoadEntries();
            HotkeyManager.RegisterMacroHotkeys(macros);
        }

        protected override void OnClosed(EventArgs e)
        {
            MacroLibrary.MacroLoaded       -= OnMacroLoaded;
            HotkeyManager.MacroHotkeyFired -= OnMacroHotkeyFired;
            HotkeyManager.UnregisterAll();
            HotkeyManager.UnregisterMacroHotkeys();
            base.OnClosed(e);
        }

        // ── Хоткеи ──────────────────────────────────────────────────────────

        private void OnHotkeyFired(string action)
        {
            ShowDebugNotification($"Горячая клавиша: {action}");
        }

        private void OnMacroHotkeyFired(string filePath)
        {
            var name = Path.GetFileNameWithoutExtension(filePath);
            MacroNameText.Text = name;
            ShowDebugNotification($"Макрос: {name}");
        }

        private void OnMacroLoaded(string filePath)
        {
            MacroNameText.Text = Path.GetFileNameWithoutExtension(filePath);
        }

        private void ShowDebugNotification(string text)
        {
            HotkeyDebugText.Text       = text;
            HotkeyDebugText.Visibility = Visibility.Visible;

            _debugTimer?.Stop();
            _debugTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _debugTimer.Tick += (s, ev) =>
            {
                HotkeyDebugText.Visibility = Visibility.Collapsed;
                _debugTimer!.Stop();
            };
            _debugTimer.Start();
        }

        // ── Управление окном ─────────────────────────────────────────────────

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState.Minimized;

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Library_Click(object sender, RoutedEventArgs e) =>
            new LibraryWindow().Show();

        private void Settings_Click(object sender, RoutedEventArgs e) =>
            new SettingsWindow().Show();

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName        = "https://github.com/NoVate911/csharp-mini-macro",
                UseShellExecute = true
            });
        }
    }
}
