using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace MiniMacro
{
    internal static class ThemeManager
    {
        public static void Apply(string theme)
        {
            bool isDark = theme switch
            {
                "Light" => false,
                "Dark"  => true,
                _       => IsSystemDarkMode()
            };

            var uri   = new Uri($"Themes/{(isDark ? "Dark" : "Light")}.xaml", UriKind.Relative);
            var dict  = new ResourceDictionary { Source = uri };
            var dicts = Application.Current.Resources.MergedDictionaries;
            var old   = dicts.FirstOrDefault(
                d => d.Source?.OriginalString.Contains("Themes/") == true);
            if (old != null) dicts.Remove(old);
            dicts.Add(dict);
        }

        private static bool IsSystemDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var val = key?.GetValue("AppsUseLightTheme");
                // 0 = тёмная, 1 = светлая; при отсутствии значения — тёмная
                return val is int i ? i == 0 : true;
            }
            catch
            {
                return true;
            }
        }
    }
}
