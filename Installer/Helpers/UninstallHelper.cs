using System;
using System.Diagnostics;
using System.IO;
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

        // Полное удаление программы. После вызова запустить ScheduleSelfDelete().
        internal static void Uninstall(string installDir, Action<string> log)
        {
            // 1. Удалить основной exe
            TryDelete(Path.Combine(installDir, "MiniMacro.exe"), "Удаление MiniMacro.exe", log);

            // 2. Ярлык на рабочем столе
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            TryDelete(Path.Combine(desktop, "Mini Macro.lnk"), "Удаление ярлыка рабочего стола", log);

            // 3. Папка меню Пуск
            var startMenuFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                "Mini Macro");
            TryDeleteDir(startMenuFolder, "Удаление папки меню Пуск", log);

            // 4. Реестровый ключ
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

        // Создаёт временный bat-файл, который удаляет uninstall.exe и папку установки
        // после закрытия деинсталлятора (через 2 секунды).
        internal static void ScheduleSelfDelete(string installDir)
        {
            var uninstallExe = Path.Combine(installDir, "uninstall.exe");
            var batPath      = Path.Combine(Path.GetTempPath(), "mm_cleanup.bat");

            var bat = string.Join(Environment.NewLine,
                "@echo off",
                "timeout /t 2 /nobreak >nul",
                $"del /f /q \"{uninstallExe}\"",
                $"rmdir /q \"{installDir}\" 2>nul",
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
