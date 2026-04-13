using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MiniMacro
{
    internal static class MacroLibrary
    {
        // Срабатывает когда пользователь загружает (выбирает) макрос
        public static event Action<string>? MacroLoaded;

        public static void SelectMacro(string filePath) =>
            MacroLoaded?.Invoke(filePath);

        // Считывает все .mmacro из папки библиотеки, возвращает список записей
        public static List<MacroEntry> LoadEntries()
        {
            var folder = SettingsManager.Current.LibraryFolder;
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return new List<MacroEntry>();

            var hotkeys = SettingsManager.Current.MacroHotkeys;
            var result  = new List<MacroEntry>();

            foreach (var file in Directory.GetFiles(folder, "*.mmacro",
                                                     SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                hotkeys.TryGetValue(name, out var hotkey);
                var (hasError, errorMsg) = ValidateFile(file);
                result.Add(new MacroEntry(file, hotkey ?? "", hasError, errorMsg));
            }

            return result;
        }

        // Сохраняет текущий макрос в файл .mmacro
        public static void SaveMacro(List<MacroAction> actions, int screenW, int screenH, string filePath)
        {
            var data = new
            {
                version      = 1,
                screenWidth  = screenW,
                screenHeight = screenH,
                actions
            };
            var json = JsonSerializer.Serialize(data,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        // Проверяет структуру .mmacro (JSON с полями version + actions[])
        private static (bool hasError, string errorMsg) ValidateFile(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                    return (true, "Файл пустой");

                var doc  = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("version", out _))
                    return (true, "Отсутствует поле «version»");

                if (!root.TryGetProperty("actions", out var actions) ||
                    actions.ValueKind != JsonValueKind.Array)
                    return (true, "Отсутствует массив «actions»");

                return (false, "");
            }
            catch (JsonException ex)
            {
                return (true, $"Ошибка JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (true, $"Ошибка чтения: {ex.Message}");
            }
        }
    }
}
