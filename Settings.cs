using System.Collections.Generic;

namespace MiniMacro
{
    public class AppSettings
    {
        public int    RepeatCount   { get; set; } = 1;
        public double PlaybackSpeed { get; set; } = 1.0;
        public string Theme         { get; set; } = "System";

        // Глобальные горячие клавиши приложения
        public string HotkeyRecord  { get; set; } = "";
        public string HotkeyPlay    { get; set; } = "";
        public string HotkeyPause   { get; set; } = "";
        public string HotkeyStop    { get; set; } = "";

        // Библиотека макросов
        public string LibraryFolder { get; set; } = "";

        // Горячие клавиши макросов: ключ = имя файла без расширения, значение = "Ctrl+F1"
        public Dictionary<string, string> MacroHotkeys { get; set; }
            = new Dictionary<string, string>();
    }
}
