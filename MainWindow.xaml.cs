using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace MiniMacro
{
    public partial class MainWindow : Window
    {
        private static MacroEngine Engine => App.Engine;

        public MainWindow()
        {
            InitializeComponent();
            MacroLibrary.MacroLoaded += OnMacroLoaded;
            Engine.StateChanged      += OnStateChanged;
            UpdateTransportUI(MacroState.Idle);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HotkeyManager.Initialize(this);
            HotkeyManager.HotkeyFired      += OnHotkeyFired;
            HotkeyManager.MacroHotkeyFired += OnMacroHotkeyFired;
            HotkeyManager.RegisterAll();

            var macros = MacroLibrary.LoadEntries();
            HotkeyManager.RegisterMacroHotkeys(macros);
        }

        protected override void OnClosed(EventArgs e)
        {
            MacroLibrary.MacroLoaded       -= OnMacroLoaded;
            Engine.StateChanged            -= OnStateChanged;
            HotkeyManager.MacroHotkeyFired -= OnMacroHotkeyFired;
            HotkeyManager.UnregisterAll();
            HotkeyManager.UnregisterMacroHotkeys();
            base.OnClosed(e);
        }

        // ── Обновление UI ────────────────────────────────────────────────────

        private void OnStateChanged(MacroState state) =>
            Dispatcher.Invoke(() => UpdateTransportUI(state));

        private void UpdateTransportUI(MacroState state)
        {
            RecordButton.IsEnabled = state == MacroState.Idle || state == MacroState.Recording;
            PlayButton.IsEnabled   = state == MacroState.Idle && Engine.HasRecording;
            PauseButton.IsEnabled  = state == MacroState.Playing || state == MacroState.Paused;
            StopButton.IsEnabled   = state != MacroState.Idle;
            SaveButton.IsEnabled   = state == MacroState.Idle && Engine.HasRecording;
            LoadButton.IsEnabled   = state == MacroState.Idle;

            StatusText.Text = state switch
            {
                MacroState.Recording => "● Запись...",
                MacroState.Playing   => "▶ Воспроизведение",
                MacroState.Paused    => "⏸ Пауза",
                _                    => ""
            };
            StatusText.Visibility = state == MacroState.Idle
                ? Visibility.Collapsed : Visibility.Visible;

            if (state == MacroState.Idle && Engine.CurrentName != null)
                MacroNameText.Text = Engine.CurrentName;
        }

        // ── Хоткеи ──────────────────────────────────────────────────────────

        private void OnHotkeyFired(string action)
        {
            if (Engine.State == MacroState.Recording)
            {
                // Во время записи только хоткей Record останавливает запись
                if (action == "Record") Engine.StopRecording();
                return;
            }

            switch (action)
            {
                case "Record": ToggleRecord(); break;
                case "Play":   DoPlay();       break;
                case "Pause":  Engine.Pause(); break;
                case "Stop":   Engine.Stop();  break;
            }
        }

        private void OnMacroHotkeyFired(string filePath)
        {
            if (Engine.State == MacroState.Recording) return;
            try
            {
                Engine.LoadFrom(filePath);
                Engine.StartPlayback();
                MacroNameText.Text = Engine.CurrentName ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске макроса:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnMacroLoaded(string filePath)
        {
            try
            {
                Engine.LoadFrom(filePath);
                MacroNameText.Text = Engine.CurrentName ?? "";
                UpdateTransportUI(Engine.State);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке макроса:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Транспортные кнопки ──────────────────────────────────────────────

        private void Record_Click(object sender, RoutedEventArgs e) => ToggleRecord();
        private void Play_Click  (object sender, RoutedEventArgs e) => DoPlay();
        private void Pause_Click (object sender, RoutedEventArgs e) => Engine.Pause();
        private void Stop_Click  (object sender, RoutedEventArgs e) => Engine.Stop();

        private void ToggleRecord()
        {
            if (Engine.State == MacroState.Idle)
            {
                Engine.StartRecording();
            }
            else if (Engine.State == MacroState.Recording)
            {
                Engine.StopRecording();
                MacroNameText.Text = Engine.HasRecording ? "Новый макрос" : "— не выбран —";
                UpdateTransportUI(MacroState.Idle);
            }
        }

        private void DoPlay()
        {
            if (Engine.State != MacroState.Idle || !Engine.HasRecording) return;
            Engine.StartPlayback();
        }

        // ── Загрузка / Сохранение ────────────────────────────────────────────

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title  = "Открыть макрос",
                Filter = "Файлы макросов (*.mmacro)|*.mmacro"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                Engine.LoadFrom(dialog.FileName);
                MacroNameText.Text = Engine.CurrentName ?? "";
                UpdateTransportUI(Engine.State);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!Engine.HasRecording) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title      = "Сохранить макрос",
                Filter     = "Файлы макросов (*.mmacro)|*.mmacro",
                DefaultExt = ".mmacro",
                FileName   = Engine.CurrentName ?? "Новый макрос"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                Engine.SaveTo(dialog.FileName);
                MacroNameText.Text = Engine.CurrentName ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
