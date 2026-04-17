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
        private bool _initialized;

        public MainWindow()
        {
            InitializeComponent();
            MacroLibrary.MacroLoaded += OnMacroLoaded;
            Engine.StateChanged      += OnStateChanged;
            UpdateTransportUI(MacroState.Idle);

            // Версия и автор из метаданных сборки
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var v   = asm.GetName().Version;
            VersionText.Text = $"v{v?.Major}.{v?.Minor}.{v?.Build}";
            AuthorText.Text  = ((System.Reflection.AssemblyCompanyAttribute?)
                Attribute.GetCustomAttribute(asm, typeof(System.Reflection.AssemblyCompanyAttribute)))
                ?.Company ?? "NoVate Source";

            // Слайдеры из настроек (до _initialized=true, чтобы не перезаписать файл)
            SideRepeatSlider.Value = SettingsManager.Current.RepeatCount;
            SideSpeedSlider.Value  = SettingsManager.Current.PlaybackSpeed;
            _initialized = true;
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

        // ── Слайдеры в сайдбаре ──────────────────────────────────────────────

        private void SideRepeat_ValueChanged(object sender,
            System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (SideRepeatText == null) return;
            int val = (int)e.NewValue;
            SideRepeatText.Text = val == 1 ? "1 раз" : $"{val} раза";
            if (!_initialized) return;
            SettingsManager.Current.RepeatCount = val;
            SettingsManager.Save();
        }

        private void SideSpeed_ValueChanged(object sender,
            System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (SideSpeedText == null) return;
            SideSpeedText.Text = $"{e.NewValue:F1}×";
            if (!_initialized) return;
            SettingsManager.Current.PlaybackSpeed = e.NewValue;
            SettingsManager.Save();
        }

        // ── Проверка обновлений ───────────────────────────────────────────────

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Блокируем интерфейс до завершения проверки обновлений
            ContentArea.IsEnabled = false;
            var result = await UpdateChecker.CheckAsync();
            ContentArea.IsEnabled = true;

            if (result.CheckFailed)
            {
                UpdateStatusText.Text       = "Нет подключения — обновления не проверены";
                UpdateStatusText.Visibility = Visibility.Visible;
                MessageBox.Show(this,
                    "Не удалось проверить наличие обновлений.\n" +
                    "Программа будет работать в текущей версии.",
                    "Нет подключения к интернету",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!result.IsUpdateAvailable) return;

            var asm = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var current = $"v{asm?.Major}.{asm?.Minor}.{asm?.Build}";

            MessageBox.Show(this,
                $"Установлена версия {current}, доступна {result.LatestVersion}.\n\n" +
                "Обновите приложение для продолжения работы.",
                "Требуется обновление",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            Process.Start(new ProcessStartInfo
                { FileName = result.ReleaseUrl, UseShellExecute = true });
            Application.Current.Shutdown();
        }
    }
}
