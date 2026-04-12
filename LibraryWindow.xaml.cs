using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MiniMacro
{
    public partial class LibraryWindow : Window
    {
        // Полный список тестовых макросов: (название, горячая клавиша)
        private readonly List<(string Name, string Hotkey)> _allMacros =
        [
            ("Приветственное сообщение", "Ctrl+F1"),
            ("Авторизация",              ""),
            ("Шаблон письма",            "Alt+E"),
            ("Ежедневный отчёт",         ""),
            ("Запуск приложений",        "Ctrl+Shift+L"),
            ("Резервное копирование",    "Ctrl+B"),
            ("Открыть браузер",          ""),
            ("Снимок экрана",            "Alt+S"),
            ("Перезагрузка сети",        ""),
            ("Очистка временных файлов", "Ctrl+Shift+D"),
            ("Переключение VPN",         ""),
            ("Ежемесячный отчёт",        "Ctrl+M"),
        ];

        public LibraryWindow()
        {
            InitializeComponent();
            // Заполнить список при открытии окна
            ApplyFilter(string.Empty);
        }

        // Фильтрация: case-insensitive, поиск подстроки в названии
        private void ApplyFilter(string query)
        {
            MacroList.Children.Clear();

            var results = string.IsNullOrWhiteSpace(query)
                ? _allMacros
                : _allMacros.Where(m =>
                    m.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

            foreach (var (name, hotkey) in results)
            {
                MacroList.Children.Add(new MacroItemControl
                {
                    MacroName  = name,
                    HotkeyText = hotkey
                });
            }

            int count = MacroList.Children.Count;

            // Показать пустое состояние если ничего не найдено
            EmptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Обновить счётчик в футере
            CountText.Text = count switch
            {
                0 => "Нет макросов",
                1 => "1 макрос",
                _ => $"{count} макросов"
            };
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(((TextBox)sender).Text);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
