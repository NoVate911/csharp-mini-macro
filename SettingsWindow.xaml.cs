using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MiniMacro
{
    public partial class SettingsWindow : Window
    {
        private Button?    _capturingButton;
        private TextBlock? _capturingText;
        private string     _capturingAction = "";

        private readonly Dictionary<Button, string> _buttonActions;

        public SettingsWindow()
        {
            InitializeComponent();

            _buttonActions = new Dictionary<Button, string>
            {
                { HkRecord, "Record" },
                { HkPlay,   "Play"   },
                { HkPause,  "Pause"  },
                { HkStop,   "Stop"   }
            };

            LoadSettings();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CancelCapture();
            Close();
        }

        // ── Загрузка настроек в UI ───────────────────────────────────────────

        private void LoadSettings()
        {
            var s = SettingsManager.Current;
            SetHotkeyText(HkRecordText, s.HotkeyRecord);
            SetHotkeyText(HkPlayText,   s.HotkeyPlay);
            SetHotkeyText(HkPauseText,  s.HotkeyPause);
            SetHotkeyText(HkStopText,   s.HotkeyStop);
            UpdateThemeButtons();
        }

        private static void SetHotkeyText(TextBlock tb, string hotkey) =>
            tb.Text = string.IsNullOrEmpty(hotkey) ? "—" : hotkey;

        // ── Тема оформления ──────────────────────────────────────────────────

        private void UpdateThemeButtons()
        {
            var active   = (Style)FindResource("ThemeButtonActiveStyle");
            var inactive = (Style)FindResource("ThemeButtonStyle");
            var theme    = SettingsManager.Current.Theme;

            DarkButton.Style   = theme == "Dark"   ? active : inactive;
            LightButton.Style  = theme == "Light"  ? active : inactive;
            SystemButton.Style = theme == "System" ? active : inactive;
        }

        private void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.Theme = "Dark";
            SettingsManager.Save();
            ThemeManager.Apply("Dark");
            UpdateThemeButtons();
        }

        private void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.Theme = "Light";
            SettingsManager.Save();
            ThemeManager.Apply("Light");
            UpdateThemeButtons();
        }

        private void SystemTheme_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.Theme = "System";
            SettingsManager.Save();
            ThemeManager.Apply("System");
            UpdateThemeButtons();
        }

        // ── Захват горячей клавиши ───────────────────────────────────────────

        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            CancelCapture();

            _capturingButton = (Button)sender;
            _capturingText   = (TextBlock)_capturingButton.Content;
            _capturingAction = _buttonActions[_capturingButton];

            _capturingButton.Tag = "capturing";
            _capturingText.Text  = "Нажмите клавишу...";
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_capturingButton == null) return;
            e.Handled = true;

            if (e.Key == Key.Escape)
            {
                // Очищаем — сохраняем пустую строку
                _capturingText!.Text = "—";
                SaveHotkey(_capturingAction, "");
            }
            else
            {
                // Определяем основную клавишу (игнорируем одиночные модификаторы)
                var mainKey = e.Key == Key.System ? e.SystemKey : e.Key;
                if (mainKey == Key.LeftCtrl  || mainKey == Key.RightCtrl  ||
                    mainKey == Key.LeftAlt   || mainKey == Key.RightAlt   ||
                    mainKey == Key.LeftShift || mainKey == Key.RightShift ||
                    mainKey == Key.LWin      || mainKey == Key.RWin)
                    return;

                // Собираем сочетание
                var parts = new System.Collections.Generic.List<string>();
                if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) parts.Add("Ctrl");
                if ((Keyboard.Modifiers & ModifierKeys.Alt)     != 0) parts.Add("Alt");
                if ((Keyboard.Modifiers & ModifierKeys.Shift)   != 0) parts.Add("Shift");
                if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) parts.Add("Win");
                parts.Add(mainKey.ToString());

                var hotkeyStr = string.Join("+", parts);
                _capturingText!.Text = hotkeyStr;
                SaveHotkey(_capturingAction, hotkeyStr);
            }

            _capturingButton.Tag = null;
            _capturingButton = null;
        }

        private static void SaveHotkey(string action, string hotkey)
        {
            var s = SettingsManager.Current;
            switch (action)
            {
                case "Record": s.HotkeyRecord = hotkey; break;
                case "Play":   s.HotkeyPlay   = hotkey; break;
                case "Pause":  s.HotkeyPause  = hotkey; break;
                case "Stop":   s.HotkeyStop   = hotkey; break;
            }
        }

        private void CancelCapture()
        {
            if (_capturingButton == null) return;
            _capturingButton.Tag = null;
            _capturingText!.Text = string.IsNullOrEmpty(GetCurrentHotkey(_capturingAction))
                                   ? "—" : GetCurrentHotkey(_capturingAction);
            _capturingButton = null;
        }

        private static string GetCurrentHotkey(string action) => action switch
        {
            "Record" => SettingsManager.Current.HotkeyRecord,
            "Play"   => SettingsManager.Current.HotkeyPlay,
            "Pause"  => SettingsManager.Current.HotkeyPause,
            "Stop"   => SettingsManager.Current.HotkeyStop,
            _        => ""
        };

        // ── Apply / Reset ────────────────────────────────────────────────────

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            CancelCapture();
            SettingsManager.Save();
            HotkeyManager.UnregisterAll();
            HotkeyManager.RegisterAll();
            Close();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            CancelCapture();
            SettingsManager.Reset();
            SettingsManager.Save();
            ThemeManager.Apply(SettingsManager.Current.Theme);
            LoadSettings();
            HotkeyManager.UnregisterAll();
            HotkeyManager.RegisterAll();
        }
    }
}
