using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace MiniMacroInstaller.Helpers
{
    internal static class RuntimeHelper
    {
        // Папка с локальными установщиками рантаймов (рядом с Setup.exe)
        private static string RuntimesFolder =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "runtimes");

        // Ожидаемые имена файлов
        private const string NetFx48File    = "ndp48.exe";
        private const string Net10x64File   = "dotnet10-x64.exe";
        private const string Net10x86File   = "dotnet10-x86.exe";

        // ─── .NET Framework 4.8 ────────────────────────────────────────────────

        // Release-код >= 528040 означает .NET FX 4.8
        internal static bool IsNetFx48Installed()
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full");
            if (key == null) return false;
            var release = key.GetValue("Release") as int?;
            return release.HasValue && release.Value >= 528040;
        }

        internal static void InstallNetFx48(Action<string> log)
        {
            var localFile = Path.Combine(RuntimesFolder, NetFx48File);

            if (File.Exists(localFile))
            {
                log("Запуск локального установщика .NET Framework 4.8...");
                RunInstaller(localFile, "/quiet /norestart", log);
                log("✓ .NET Framework 4.8 установлен.");
            }
            else
            {
                log($"! Файл {NetFx48File} не найден в assets/runtimes/.");
                log("  Поместите установщик .NET Framework 4.8 в папку assets/runtimes/ рядом с Setup.exe.");
                throw new FileNotFoundException(
                    $"Установщик .NET Framework 4.8 не найден: {localFile}");
            }
        }

        // ─── .NET Desktop Runtime 10.0 (x64) ──────────────────────────────────

        internal static bool IsNet10x64Installed()
        {
            var basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "dotnet", "shared", "Microsoft.WindowsDesktop.App");
            return ContainsVersion10(basePath);
        }

        internal static void InstallNet10x64(Action<string> log)
        {
            var localFile = Path.Combine(RuntimesFolder, Net10x64File);

            if (File.Exists(localFile))
            {
                log("Запуск локального установщика .NET Desktop Runtime 10.0 (x64)...");
                RunInstaller(localFile, "/install /quiet /norestart", log);
                log("✓ .NET Desktop Runtime 10.0 (x64) установлен.");
            }
            else
            {
                log($"! Файл {Net10x64File} не найден в assets/runtimes/.");
                log("  Поместите установщик .NET Desktop Runtime 10.0 x64 в папку assets/runtimes/ рядом с Setup.exe.");
                throw new FileNotFoundException(
                    $"Установщик .NET Desktop Runtime 10.0 x64 не найден: {localFile}");
            }
        }

        // ─── .NET Desktop Runtime 10.0 (x86) ──────────────────────────────────

        internal static bool IsNet10x86Installed()
        {
            // x86-рантаймы живут в Program Files (x86)
            var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var basePath = Path.Combine(pf86, "dotnet", "shared", "Microsoft.WindowsDesktop.App");
            return ContainsVersion10(basePath);
        }

        internal static void InstallNet10x86(Action<string> log)
        {
            var localFile = Path.Combine(RuntimesFolder, Net10x86File);

            if (File.Exists(localFile))
            {
                log("Запуск локального установщика .NET Desktop Runtime 10.0 (x86)...");
                RunInstaller(localFile, "/install /quiet /norestart", log);
                log("✓ .NET Desktop Runtime 10.0 (x86) установлен.");
            }
            else
            {
                log($"! Файл {Net10x86File} не найден в assets/runtimes/.");
                log("  Поместите установщик .NET Desktop Runtime 10.0 x86 в папку assets/runtimes/ рядом с Setup.exe.");
                throw new FileNotFoundException(
                    $"Установщик .NET Desktop Runtime 10.0 x86 не найден: {localFile}");
            }
        }

        // ─── Вспомогательные методы ────────────────────────────────────────────

        private static bool ContainsVersion10(string basePath)
        {
            if (!Directory.Exists(basePath)) return false;
            foreach (var dir in Directory.GetDirectories(basePath))
            {
                if (Path.GetFileName(dir).StartsWith("10.", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static void RunInstaller(string filePath, string arguments, Action<string> log)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName        = filePath,
                Arguments       = arguments,
                UseShellExecute = true,
                Verb            = "runas"
            });
            process?.WaitForExit();

            if (process?.ExitCode != 0 && process?.ExitCode != 3010)
                log($"  ! Установщик завершился с кодом {process?.ExitCode} (3010 = требуется перезагрузка).");
        }
    }
}
