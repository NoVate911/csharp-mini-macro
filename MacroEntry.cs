using System.IO;

namespace MiniMacro
{
    public class MacroEntry
    {
        public string FilePath     { get; }
        public string Name         { get; }   // имя файла без расширения
        public string Hotkey       { get; set; }
        public bool   HasError     { get; }
        public string ErrorMessage { get; }

        public MacroEntry(string filePath, string hotkey,
                          bool hasError = false, string errorMessage = "")
        {
            FilePath     = filePath;
            Name         = Path.GetFileNameWithoutExtension(filePath);
            Hotkey       = hotkey;
            HasError     = hasError;
            ErrorMessage = errorMessage;
        }
    }
}
