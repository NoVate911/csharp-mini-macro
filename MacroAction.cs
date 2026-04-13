namespace MiniMacro
{
    public enum MacroActionType
    {
        KeyDown,
        KeyUp,
        MouseMove,
        MouseDown,
        MouseUp,
        MouseWheel
    }

    public class MacroAction
    {
        public MacroActionType Type      { get; set; }
        public long            Timestamp { get; set; }  // мс от начала записи

        // Клавиатура
        public int VirtualKey { get; set; }
        public int ScanCode   { get; set; }

        // Мышь
        public double  NormX   { get; set; }   // 0.0–1.0 от ширины экрана
        public double  NormY   { get; set; }   // 0.0–1.0 от высоты экрана
        public string? Button  { get; set; }   // "Left", "Right", "Middle"
        public int     Delta   { get; set; }   // для MouseWheel
    }
}
