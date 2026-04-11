using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MiniMacro
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Перетаскивание окна за заголовок
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
            Close();
        }

        // Открытие репозитория в браузере по умолчанию
        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/NoVate911/csharp-mini-macro",
                UseShellExecute = true
            });
        }
    }
}
