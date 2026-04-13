using System;
using System.Runtime.InteropServices;

namespace MiniMacroInstaller.Helpers
{
    internal static class OsHelper
    {
        // Возвращает true если ОС Windows 10 или новее
        internal static bool IsWindows10OrNewer()
        {
            var version = Environment.OSVersion.Version;
            // Windows 10 → Major=10, Minor=0, Build>=10240
            return version.Major >= 10;
        }

        internal static string GetOsDisplayName()
        {
            var v = Environment.OSVersion.Version;
            if (v.Major == 10 && v.Build >= 22000) return "Windows 11";
            if (v.Major == 10) return "Windows 10";
            if (v.Major == 6 && v.Minor == 3) return "Windows 8.1";
            if (v.Major == 6 && v.Minor == 2) return "Windows 8";
            if (v.Major == 6 && v.Minor == 1) return "Windows 7";
            return $"Windows {v.Major}.{v.Minor}";
        }
    }
}
