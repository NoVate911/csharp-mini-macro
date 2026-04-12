using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MiniMacro
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
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

        private void RepeatSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RepeatValueText == null) return;
            int value = (int)e.NewValue;
            RepeatValueText.Text = value == 1 ? "1 раз" : $"{value} раза";
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SpeedValueText == null) return;
            SpeedValueText.Text = $"{e.NewValue:F1}×";
        }
    }
}
