using System;
using System.IO;
using System.Text.Json;

namespace MiniMacro
{
    internal static class SettingsManager
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MiniMacro", "settings.json");

        internal static AppSettings Current { get; private set; } = new AppSettings();

        internal static void Reset() => Current = new AppSettings();

        internal static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath))
                              ?? new AppSettings();
            }
            catch { Current = new AppSettings(); }
        }

        internal static void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Current,
                new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
