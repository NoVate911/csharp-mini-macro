using System;
using System.Windows;
using System.Windows.Controls;

namespace MiniMacro
{
    public partial class MacroItemControl : UserControl
    {
        // ── Dependency Properties ────────────────────────────────────────────

        public static readonly DependencyProperty MacroNameProperty =
            DependencyProperty.Register(nameof(MacroName), typeof(string),
                typeof(MacroItemControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty HotkeyTextProperty =
            DependencyProperty.Register(nameof(HotkeyText), typeof(string),
                typeof(MacroItemControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register(nameof(FilePath), typeof(string),
                typeof(MacroItemControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(nameof(HasError), typeof(bool),
                typeof(MacroItemControl), new PropertyMetadata(false));

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(nameof(ErrorMessage), typeof(string),
                typeof(MacroItemControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsCapturingProperty =
            DependencyProperty.Register(nameof(IsCapturing), typeof(bool),
                typeof(MacroItemControl), new PropertyMetadata(false));

        // ── CLR свойства ─────────────────────────────────────────────────────

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

        public string FilePath
        {
            get => (string)GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        public bool HasError
        {
            get => (bool)GetValue(HasErrorProperty);
            set => SetValue(HasErrorProperty, value);
        }

        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        public bool IsCapturing
        {
            get => (bool)GetValue(IsCapturingProperty);
            set => SetValue(IsCapturingProperty, value);
        }

        // ── События ──────────────────────────────────────────────────────────

        public event EventHandler? LoadRequested;
        public event EventHandler? OverwriteRequested;
        public event EventHandler? HotkeyRequested;
        public event EventHandler? DeleteRequested;

        // ── Конструктор ──────────────────────────────────────────────────────

        public MacroItemControl()
        {
            InitializeComponent();
        }

        // ── Обработчики кнопок ───────────────────────────────────────────────

        private void Load_Click(object sender, RoutedEventArgs e)     => LoadRequested?.Invoke(this, EventArgs.Empty);
        private void Overwrite_Click(object sender, RoutedEventArgs e) => OverwriteRequested?.Invoke(this, EventArgs.Empty);
        private void SetHotkey_Click(object sender, RoutedEventArgs e) => HotkeyRequested?.Invoke(this, EventArgs.Empty);
        private void Delete_Click(object sender, RoutedEventArgs e)    => DeleteRequested?.Invoke(this, EventArgs.Empty);
    }
}
