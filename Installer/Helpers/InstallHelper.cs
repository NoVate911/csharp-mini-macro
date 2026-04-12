using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace MiniMacroInstaller.Helpers
{
    internal static class InstallHelper
    {
        // Имена файлов exe рядом с установщиком (в папке assets/)
        private const string ModernExe = "MiniMacro-modern.exe";
        private const string LegacyExe = "MiniMacro-legacy.exe";

        // Папка assets/ рядом с Setup.exe
        internal static string AssetsFolder =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");

        internal static bool AssetExists(bool isModern)
        {
            var file = isModern ? ModernExe : LegacyExe;
            return File.Exists(Path.Combine(AssetsFolder, file));
        }

        // Устанавливает программу в указанную папку
        internal static void Install(string installDir, bool isModern,
                                     bool desktopShortcut, bool startMenuShortcut,
                                     Action<string> log)
        {
            var srcExe = Path.Combine(AssetsFolder, isModern ? ModernExe : LegacyExe);
            var dstExe = Path.Combine(installDir, "MiniMacro.exe");

            // Создать папку
            log($"Создание папки: {installDir}");
            Directory.CreateDirectory(installDir);

            // Копировать exe
            log("Копирование файлов...");
            File.Copy(srcExe, dstExe, overwrite: true);

            // Ярлык на рабочем столе
            if (desktopShortcut)
            {
                log("Создание ярлыка на рабочем столе...");
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                CreateShortcut(dstExe, Path.Combine(desktop, "Mini Macro.lnk"),
                               "Лёгкая программа для записи и воспроизведения макросов");
            }

            // Ярлык в меню Пуск
            if (startMenuShortcut)
            {
                log("Создание ярлыка в меню Пуск...");
                var startMenu = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                    "Mini Macro");
                Directory.CreateDirectory(startMenu);
                CreateShortcut(dstExe, Path.Combine(startMenu, "Mini Macro.lnk"),
                               "Лёгкая программа для записи и воспроизведения макросов");
            }

            // Запись в реестр для «Программы и компоненты»
            log("Регистрация в системе...");
            RegisterUninstall(installDir, dstExe, isModern);

            log("✓ Установка завершена.");
        }

        internal static void LaunchApp(string installDir)
        {
            var exe = Path.Combine(installDir, "MiniMacro.exe");
            if (File.Exists(exe))
                Process.Start(exe);
        }

        // ── Приватные хелперы ────────────────────────────────────────────────

        private static void CreateShortcut(string targetPath, string shortcutPath, string description)
        {
            // Используем COM-объект WScript.Shell без явной COM-ссылки
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.Description = description;
            shortcut.Save();
        }

        private static void RegisterUninstall(string installDir, string exePath, bool isModern)
        {
            const string keyPath =
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MiniMacro";

            using var key = Registry.LocalMachine.CreateSubKey(keyPath);
            if (key == null) return;

            key.SetValue("DisplayName",          "Mini Macro");
            key.SetValue("DisplayVersion",       "0.1.0");
            key.SetValue("Publisher",            "NoVate Source");
            key.SetValue("InstallLocation",      installDir);
            key.SetValue("UninstallString",      $"\"{exePath}\" --uninstall");
            key.SetValue("DisplayIcon",          exePath);
            key.SetValue("NoModify",             1, RegistryValueKind.DWord);
            key.SetValue("NoRepair",             1, RegistryValueKind.DWord);
            key.SetValue("Comments",
                isModern ? "Версия Modern (.NET 10.0)" : "Версия Legacy (.NET Framework 4.8)");
        }
    }
}
