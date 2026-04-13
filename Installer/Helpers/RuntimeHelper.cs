using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Microsoft.Win32;

namespace MiniMacroInstaller.Helpers
{
    internal static class RuntimeHelper
    {
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

        // Загружает и запускает веб-установщик .NET FX 4.8
        internal static void InstallNetFx48(Action<string> log)
        {
            const string url = "https://go.microsoft.com/fwlink/?LinkId=2085155";
            var tempPath = Path.Combine(Path.GetTempPath(), "ndp48-web.exe");

            log("Загрузка .NET Framework 4.8...");
            using var client = new WebClient();
            client.DownloadFile(url, tempPath);

            log("Запуск установщика .NET Framework 4.8...");
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                Arguments = "/quiet /norestart",
                UseShellExecute = true,
                Verb = "runas"
            });
            process?.WaitForExit();
            log("Установка .NET Framework 4.8 завершена.");
        }

        // ─── .NET Desktop Runtime 10.0 ─────────────────────────────────────────

        internal static bool IsNet10Installed()
        {
            // Проверяем наличие папки с версией 10.x в стандартном расположении
            var basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "dotnet", "shared", "Microsoft.WindowsDesktop.App");

            if (!Directory.Exists(basePath)) return false;
            foreach (var dir in Directory.GetDirectories(basePath))
            {
                var name = Path.GetFileName(dir);
                if (name.StartsWith("10.", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        // Устанавливает .NET 10.0 Desktop Runtime через winget или открывает страницу загрузки
        internal static void InstallNet10(Action<string> log)
        {
            log("Попытка установки через winget...");
            try
            {
                var winget = Process.Start(new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "install --id Microsoft.DotNet.DesktopRuntime.10 --silent --accept-package-agreements --accept-source-agreements",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });

                if (winget != null)
                {
                    var output = winget.StandardOutput.ReadToEnd();
                    winget.WaitForExit();

                    if (winget.ExitCode == 0)
                    {
                        log("✓ .NET Desktop Runtime 10 установлен через winget.");
                        return;
                    }
                    log($"winget завершился с кодом {winget.ExitCode}. Открываем страницу загрузки...");
                }
            }
            catch
            {
                log("winget недоступен. Открываем страницу загрузки...");
            }

            // Резервный вариант: открыть страницу загрузки
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://dotnet.microsoft.com/en-us/download/dotnet/10.0",
                UseShellExecute = true
            });
            log("Загрузите и установите '.NET Desktop Runtime 10.x', затем перезапустите установщик.");
        }
    }
}
