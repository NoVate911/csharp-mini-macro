using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace MiniMacroInstaller.Helpers
{
    internal static class InstallHelper
    {
        // Подпапки внутри assets/ для каждой версии
        private const string ModernFolder = "modern";
        private const string LegacyFolder = "legacy";

        // Папка assets/ рядом с Setup.exe
        internal static string AssetsFolder =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");

        // Проверяем наличие MiniMacro.exe в нужной подпапке
        internal static bool AssetExists(bool isModern)
        {
            var folder = Path.Combine(AssetsFolder, isModern ? ModernFolder : LegacyFolder);
            return File.Exists(Path.Combine(folder, "MiniMacro.exe"));
        }

        // Устанавливает программу в указанную папку
        internal static void Install(string installDir, bool isModern,
                                     bool desktopShortcut, bool startMenuShortcut,
                                     Action<string> log)
        {
            var srcFolder = Path.Combine(AssetsFolder, isModern ? ModernFolder : LegacyFolder);
            var dstExe    = Path.Combine(installDir, "MiniMacro.exe");

            log($"Создание папки: {installDir}");
            Directory.CreateDirectory(installDir);

            // Копируем все файлы из папки assets/modern/ или assets/legacy/
            // Modern требует: MiniMacro.exe + MiniMacro.dll + .deps.json + .runtimeconfig.json
            // Legacy требует: только MiniMacro.exe
            log("Копирование файлов...");
            foreach (var srcFile in Directory.GetFiles(srcFolder))
            {
                var fileName = Path.GetFileName(srcFile);
                File.Copy(srcFile, Path.Combine(installDir, fileName), overwrite: true);
                log($"  {fileName}");
            }

            // Копируем себя в папку установки как деинсталлятор
            log("Копирование деинсталлятора...");
            var setupExe     = Assembly.GetEntryAssembly()?.Location
                               ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MiniMacroSetup.exe");
            var uninstallExe = Path.Combine(installDir, "uninstall.exe");
            if (File.Exists(setupExe))
                File.Copy(setupExe, uninstallExe, overwrite: true);

            if (desktopShortcut)
            {
                log("Создание ярлыка на рабочем столе...");
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                CreateShortcut(dstExe, Path.Combine(desktop, "Mini Macro.lnk"),
                               "Лёгкая программа для записи и воспроизведения макросов", log);
            }

            if (startMenuShortcut)
            {
                log("Создание ярлыка в меню Пуск...");
                var startMenu = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                    "Mini Macro");
                Directory.CreateDirectory(startMenu);
                CreateShortcut(dstExe, Path.Combine(startMenu, "Mini Macro.lnk"),
                               "Лёгкая программа для записи и воспроизведения макросов", log);
            }

            log("Регистрация в системе...");
            RegisterUninstall(installDir, dstExe, uninstallExe, isModern);

            log("✓ Установка завершена.");
        }

        internal static void LaunchApp(string installDir)
        {
            var exe = Path.Combine(installDir, "MiniMacro.exe");
            if (!File.Exists(exe)) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName         = exe,
                    UseShellExecute  = true,
                    WorkingDirectory = installDir
                });
            }
            catch { }
        }

        // ── Приватные хелперы ────────────────────────────────────────────────

        private static void CreateShortcut(string targetPath, string shortcutPath,
                                           string description, Action<string> log)
        {
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    log($"  ! WScript.Shell недоступен — ярлык не создан: {shortcutPath}");
                    return;
                }
                dynamic shell    = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath       = targetPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                shortcut.Description      = description;
                shortcut.Save();

                if (!File.Exists(shortcutPath))
                    log($"  ! Ярлык не был создан: {shortcutPath}");
            }
            catch (Exception ex)
            {
                log($"  ! Ошибка создания ярлыка: {ex.Message}");
            }
        }

        private static void RegisterUninstall(string installDir, string exePath,
                                              string uninstallExe, bool isModern)
        {
            const string keyPath =
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MiniMacro";

            using var key = Registry.LocalMachine.CreateSubKey(keyPath);
            if (key == null) return;

            key.SetValue("DisplayName",     "Mini Macro");
            key.SetValue("DisplayVersion",  "0.1.0");
            key.SetValue("Publisher",       "NoVate Source");
            key.SetValue("InstallLocation", installDir);
            key.SetValue("UninstallString", $"\"{uninstallExe}\" --uninstall");
            key.SetValue("DisplayIcon",     exePath);
            key.SetValue("NoModify",        1, RegistryValueKind.DWord);
            key.SetValue("NoRepair",        1, RegistryValueKind.DWord);
            key.SetValue("Comments",
                isModern ? "Версия Modern (.NET 10.0)" : "Версия Legacy (.NET Framework 4.8)");
        }
    }
}
