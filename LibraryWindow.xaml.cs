using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MiniMacro
{
    public partial class LibraryWindow : Window
    {
        private List<MacroEntry> _allMacros = new List<MacroEntry>();

        // Отображаемые элементы: item → соответствующий MacroEntry
        private readonly Dictionary<MacroItemControl, MacroEntry> _itemMap
            = new Dictionary<MacroItemControl, MacroEntry>();

        // Состояние захвата горячей клавиши
        private MacroItemControl? _capturingItem;
        private string            _savedHotkey = "";

        public LibraryWindow()
        {
            InitializeComponent();

            var folder = SettingsManager.Current.LibraryFolder;
            FolderPathText.Text = string.IsNullOrEmpty(folder) ? "Папка не выбрана" : folder;

            LoadMacros();
        }

        // ── Загрузка и фильтрация ────────────────────────────────────────────

        private void LoadMacros()
        {
            _allMacros = MacroLibrary.LoadEntries();

            // Перерегистрируем хоткеи макросов
            HotkeyManager.UnregisterMacroHotkeys();
            HotkeyManager.RegisterMacroHotkeys(_allMacros);

            ApplyFilter(SearchBox?.Text ?? string.Empty);
        }

        private void ApplyFilter(string query)
        {
            CancelCapture();
            MacroList.Children.Clear();
            _itemMap.Clear();

            IEnumerable<MacroEntry> results = string.IsNullOrWhiteSpace(query)
                ? _allMacros
                : _allMacros.FindAll(m => ContainsIgnoreCase(m.Name, query));

            foreach (var entry in results)
            {
                var item = new MacroItemControl
                {
                    MacroName    = entry.Name,
                    HotkeyText   = entry.Hotkey,
                    FilePath     = entry.FilePath,
                    HasError     = entry.HasError,
                    ErrorMessage = entry.ErrorMessage
                };

                // Захват локальных ссылок для лямбд
                var e = entry;
                var i = item;
                item.LoadRequested     += (s, _) => OnLoad(e);
                item.OverwriteRequested += (s, _) => OnOverwrite(e);
                item.HotkeyRequested   += (s, _) => OnHotkeyRequested(i, e);
                item.DeleteRequested   += (s, _) => OnDelete(i, e);

                _itemMap[item] = entry;
                MacroList.Children.Add(item);
            }

            UpdateFooter();
        }

        private void UpdateFooter()
        {
            int count = MacroList.Children.Count;

            var folderEmpty = string.IsNullOrEmpty(SettingsManager.Current.LibraryFolder);
            EmptyState.Visibility  = count == 0 ? Visibility.Visible : Visibility.Collapsed;
            EmptyStateText.Text    = folderEmpty
                ? "Выберите папку с макросами"
                : (string.IsNullOrWhiteSpace(SearchBox?.Text)
                    ? "Папка не содержит макросов"
                    : "Ничего не найдено");

            CountText.Text = count == 0 ? string.Empty
                           : count == 1 ? "1 макрос"
                           : $"{count} макросов";
        }

        // ── Действия с макросами ─────────────────────────────────────────────

        private void OnLoad(MacroEntry entry) =>
            MacroLibrary.SelectMacro(entry.FilePath);

        private void OnOverwrite(MacroEntry entry)
        {
            var engine = App.Engine;
            if (!engine.HasRecording)
            {
                MessageBox.Show("Нет записанного макроса для перезаписи.",
                    "Перезапись", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Перезаписать «{entry.Name}» текущим макросом?",
                "Перезапись макроса",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                engine.SaveTo(entry.FilePath);
                LoadMacros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при перезаписи:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnHotkeyRequested(MacroItemControl item, MacroEntry entry)
        {
            CancelCapture();
            _capturingItem = item;
            _savedHotkey   = entry.Hotkey;

            item.HotkeyText   = "Нажмите...";
            item.IsCapturing  = true;
            CaptureHintText.Text       = $"Назначение для «{entry.Name}»  ·  ESC — очистить  ·  Esc дважды — отмена";
            CaptureHintText.Visibility = Visibility.Visible;
        }

        private void OnDelete(MacroItemControl item, MacroEntry entry)
        {
            CancelCapture();

            var result = MessageBox.Show(
                $"Удалить макрос «{entry.Name}»?\n\nФайл будет удалён без возможности восстановления.",
                "Удаление макроса",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                File.Delete(entry.FilePath);
                SettingsManager.Current.MacroHotkeys.Remove(entry.Name);
                SettingsManager.Save();
                LoadMacros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении файла:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Захват горячей клавиши ───────────────────────────────────────────

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_capturingItem == null) return;
            e.Handled = true;

            if (e.Key == Key.Escape)
            {
                // Первый Escape: очищает хоткей макроса
                // (повторный не придёт — после первого _capturingItem = null)
                CommitHotkey(_capturingItem, "");
                return;
            }

            var mainKey = e.Key == Key.System ? e.SystemKey : e.Key;
            if (IsModifierOnly(mainKey)) return; // ждём основную клавишу

            var parts = new List<string>();
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) parts.Add("Ctrl");
            if ((Keyboard.Modifiers & ModifierKeys.Alt)     != 0) parts.Add("Alt");
            if ((Keyboard.Modifiers & ModifierKeys.Shift)   != 0) parts.Add("Shift");
            if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) parts.Add("Win");
            parts.Add(mainKey.ToString());

            var hotkeyStr = string.Join("+", parts);

            var entry = _itemMap[_capturingItem];
            if (HotkeyManager.IsHotkeyUsed(hotkeyStr, entry.Name))
            {
                MessageBox.Show(
                    $"Горячая клавиша «{hotkeyStr}» уже используется другим действием или макросом.",
                    "Конфликт горячих клавиш",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CancelCapture();
                return;
            }

            CommitHotkey(_capturingItem, hotkeyStr);
        }

        private void CommitHotkey(MacroItemControl item, string hotkey)
        {
            var entry = _itemMap[item];
            entry.Hotkey = hotkey;

            if (string.IsNullOrEmpty(hotkey))
                SettingsManager.Current.MacroHotkeys.Remove(entry.Name);
            else
                SettingsManager.Current.MacroHotkeys[entry.Name] = hotkey;

            SettingsManager.Save();

            HotkeyManager.UnregisterMacroHotkeys();
            HotkeyManager.RegisterMacroHotkeys(_allMacros);

            item.HotkeyText   = hotkey;
            item.IsCapturing  = false;
            _capturingItem    = null;
            CaptureHintText.Visibility = Visibility.Collapsed;
        }

        private void CancelCapture()
        {
            if (_capturingItem == null) return;
            _capturingItem.HotkeyText  = _savedHotkey;
            _capturingItem.IsCapturing = false;
            _capturingItem = null;
            CaptureHintText.Visibility = Visibility.Collapsed;
        }

        private static bool IsModifierOnly(Key key) =>
            key == Key.LeftCtrl  || key == Key.RightCtrl  ||
            key == Key.LeftAlt   || key == Key.RightAlt   ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin      || key == Key.RWin;

        // ── Смена папки ──────────────────────────────────────────────────────

        private void ChangeFolder_Click(object sender, RoutedEventArgs e)
        {
            string? newFolder = PickFolder();
            if (newFolder == null) return;

            SettingsManager.Current.LibraryFolder = newFolder;
            SettingsManager.Save();
            FolderPathText.Text = newFolder;
            LoadMacros();
        }

        private static string? PickFolder()
        {
#if LEGACY
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Выберите папку с макросами"
            };
            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                ? dialog.SelectedPath : null;
#else
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Выберите папку с макросами"
            };
            return dialog.ShowDialog() == true ? dialog.FolderName : null;
#endif
        }

        // ── Прочие обработчики ───────────────────────────────────────────────

        private void Search_TextChanged(object sender, TextChangedEventArgs e) =>
            ApplyFilter(((TextBox)sender).Text);

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

        // ── Вспомогательное ──────────────────────────────────────────────────

        private static bool ContainsIgnoreCase(string source, string value)
        {
#if LEGACY
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
#else
            return source.Contains(value, StringComparison.OrdinalIgnoreCase);
#endif
        }
    }
}
