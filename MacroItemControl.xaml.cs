using System.Windows;
using System.Windows.Controls;

namespace MiniMacro
{
    public partial class MacroItemControl : UserControl
    {
        // Название макроса (отображается в списке)
        public static readonly DependencyProperty MacroNameProperty =
            DependencyProperty.Register(
                nameof(MacroName), typeof(string), typeof(MacroItemControl),
                new PropertyMetadata(string.Empty));

        // Горячая клавиша; пустая строка = badge скрыт
        public static readonly DependencyProperty HotkeyTextProperty =
            DependencyProperty.Register(
                nameof(HotkeyText), typeof(string), typeof(MacroItemControl),
                new PropertyMetadata(string.Empty));

        public string MacroName
        {
            get => (string)GetValue(MacroNameProperty);
            set => SetValue(MacroNameProperty, value);
        }

        public string HotkeyText
        {
            get => (string)GetValue(HotkeyTextProperty);
            set => SetValue(HotkeyTextProperty, value);
        }

        public MacroItemControl()
        {
            InitializeComponent();
        }
    }
}
