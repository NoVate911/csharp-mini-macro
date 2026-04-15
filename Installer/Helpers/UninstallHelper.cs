using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MiniMacroInstaller.Helpers
{
    internal static class UninstallHelper
    {
        private const string RegistryKey =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MiniMacro";

        // Читает путь установки из реестра
        internal static string? GetInstallDir()
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryKey);
            return key?.GetValue("InstallLocation") as string;
        }

        // Читает путь к папке библиотеки макросов из файла настроек приложения
        internal static string? GetLibraryFolder()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MiniMacro", "settings.json");

                if (!File.Exists(settingsPath)) return null;

                var json  = File.ReadAllText(settingsPath);
                var match = Regex.Match(json, "\"LibraryFolder\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
                if (!match.Success) return null;

                var folder = match.Groups[1].Value.Replace("\\\\", "\\");
                return string.IsNullOrWhiteSpace(folder) ? null : folder;
            }
            catch
            {
                return null;
            }
        }

        // Полное удаление программы. После вызова вызвать ScheduleSelfDelete().
        internal static void Uninstall(string installDir, bool deleteMacros, Action<string> log)
        {
            // 1. Удалить все файлы из папки установки (кроме uninstall.exe — он удаляется батником)
            log("Удаление файлов программы...");
            try
            {
                foreach (var file in Directory.GetFiles(installDir))
                {
                    var name = Path.GetFileName(file);
                    if (name.Equals("uninstall.exe", StringComparison.OrdinalIgnoreCase))
                        continue;
                    TryDelete(file, $"  {name}", log);
                }
                // Подпапки (например, runtimes/ для net10)
                foreach (var dir in Directory.GetDirectories(installDir))
                    TryDeleteDir(dir, $"  {Path.GetFileName(dir)}/", log);
            }
            catch (Exception ex)
            {
                log($"  ! {ex.Message}");
            }

            // 2. Ярлык на рабочем столе
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            TryDelete(Path.Combine(desktop, "Mini Macro.lnk"), "Удаление ярлыка рабочего стола", log);

            // 3. Папка меню Пуск
            var startMenuFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                "Mini Macro");
            TryDeleteDir(startMenuFolder, "Удаление папки меню Пуск", log);

            // 4. Папка настроек AppData
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MiniMacro");
            TryDeleteDir(appDataPath, "Удаление данных приложения (AppData)", log);

            // 5. Макросы из библиотеки (если пользователь выбрал)
            if (deleteMacros)
            {
                var libraryFolder = GetLibraryFolder();
                if (!string.IsNullOrEmpty(libraryFolder) && Directory.Exists(libraryFolder))
                    TryDeleteDir(libraryFolder!, "Удаление папки макросов", log);
            }

            // 6. Реестровый ключ
            log("Удаление записи из реестра...");
            try
            {
                Registry.LocalMachine.DeleteSubKeyTree(RegistryKey, throwOnMissingSubKey: false);
            }
            catch (Exception ex)
            {
                log($"  ! Ошибка реестра: {ex.Message}");
            }

            log("✓ Удаление завершено.");
        }

        // Создаёт временный bat-файл, который удаляет uninstall.exe и всю папку установки
        // после закрытия деинсталлятора (через 2 секунды).
        internal static void ScheduleSelfDelete(string installDir)
        {
            var uninstallExe = Path.Combine(installDir, "uninstall.exe");
            var batPath      = Path.Combine(Path.GetTempPath(), "mm_cleanup.bat");

            var bat = string.Join(Environment.NewLine,
                "@echo off",
                "timeout /t 2 /nobreak >nul",
                $"del /f /q \"{uninstallExe}\"",
                $"rmdir /s /q \"{installDir}\" 2>nul",
                $"del \"%~f0\""
            );

            File.WriteAllText(batPath, bat);

            Process.Start(new ProcessStartInfo
            {
                FileName        = batPath,
                UseShellExecute = false,
                CreateNoWindow  = true,
                WindowStyle     = ProcessWindowStyle.Hidden
            });
        }

        // ── Приватные хелперы ────────────────────────────────────────────────

        private static void TryDelete(string path, string description, Action<string> log)
        {
            log($"{description}...");
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                log($"  ! {ex.Message}");
            }
        }

        private static void TryDeleteDir(string path, string description, Action<string> log)
        {
            log($"{description}...");
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch (Exception ex)
            {
                log($"  ! {ex.Message}");
            }
        }
    }
}
