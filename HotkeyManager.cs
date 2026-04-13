using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace MiniMacro
{
    internal static class HotkeyManager
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_ALT     = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT   = 0x0004;
        private const uint MOD_WIN     = 0x0008;
        private const int  WM_HOTKEY   = 0x0312;

        // Глобальные хоткеи: ID 1–4
        private static readonly Dictionary<int, string> _idToAction =
            new Dictionary<int, string>
            {
                { 1, "Record" }, { 2, "Play" }, { 3, "Pause" }, { 4, "Stop" }
            };

        // Хоткеи макросов: ID 100+
        private static readonly Dictionary<int, string> _idToMacroFile =
            new Dictionary<int, string>();

        private static IntPtr _hwnd;

        // Срабатывает для глобальных хоткеев (action = "Record"/"Play"/"Pause"/"Stop")
        public static event Action<string>? HotkeyFired;

        // Срабатывает для хоткеев макросов (filePath = полный путь к .mmacro)
        public static event Action<string>? MacroHotkeyFired;

        public static void Initialize(Window window)
        {
            _hwnd = new WindowInteropHelper(window).Handle;
            HwndSource.FromHwnd(_hwnd)?.AddHook(WndProc);
        }

        // ── Глобальные хоткеи ────────────────────────────────────────────────

        public static void RegisterAll()
        {
            var s = SettingsManager.Current;
            Register(1, s.HotkeyRecord);
            Register(2, s.HotkeyPlay);
            Register(3, s.HotkeyPause);
            Register(4, s.HotkeyStop);
        }

        public static void UnregisterAll()
        {
            for (int i = 1; i <= 4; i++)
                UnregisterHotKey(_hwnd, i);
        }

        // ── Хоткеи макросов ──────────────────────────────────────────────────

        public static void RegisterMacroHotkeys(IEnumerable<MacroEntry> entries)
        {
            int id = 100;
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Hotkey)) continue;
                ParseHotkey(entry.Hotkey, out uint mods, out uint vk);
                if (vk == 0) continue;

                if (RegisterHotKey(_hwnd, id, mods, vk))
                    _idToMacroFile[id] = entry.FilePath;

                id++;
            }
        }

        public static void UnregisterMacroHotkeys()
        {
            foreach (var id in _idToMacroFile.Keys)
                UnregisterHotKey(_hwnd, id);
            _idToMacroFile.Clear();
        }

        // ── Утилиты ──────────────────────────────────────────────────────────

        // Проверяет, занят ли хоткей (глобальным или любым макросом).
        // excludeMacroName — имя файла (без расширения), хоткей которого игнорируется.
        public static bool IsHotkeyUsed(string hotkey, string? excludeMacroName = null)
        {
            if (string.IsNullOrEmpty(hotkey)) return false;

            var s = SettingsManager.Current;

            // Глобальные хоткеи приложения
            if (s.HotkeyRecord == hotkey || s.HotkeyPlay  == hotkey ||
                s.HotkeyPause  == hotkey || s.HotkeyStop  == hotkey)
                return true;

            // Хоткеи других макросов
            foreach (var kv in s.MacroHotkeys)
                if (kv.Key != excludeMacroName && kv.Value == hotkey)
                    return true;

            return false;
        }

        // Парсит "Ctrl+F1" → mods=MOD_CONTROL, vk=VK_F1
        public static void ParseHotkey(string hotkey, out uint mods, out uint vk)
        {
            mods = 0;
            vk   = 0;
            if (string.IsNullOrEmpty(hotkey)) return;

            var parts = hotkey.Split('+');
            foreach (var part in parts)
            {
                switch (part.Trim().ToLowerInvariant())
                {
                    case "ctrl":  mods |= MOD_CONTROL; break;
                    case "alt":   mods |= MOD_ALT;     break;
                    case "shift": mods |= MOD_SHIFT;   break;
                    case "win":   mods |= MOD_WIN;      break;
                    default:
                        if (Enum.TryParse<Key>(part.Trim(), true, out var key))
                            vk = (uint)KeyInterop.VirtualKeyFromKey(key);
                        break;
                }
            }
        }

        // ── WndProc ──────────────────────────────────────────────────────────

        private static void Register(int id, string hotkey)
        {
            if (string.IsNullOrEmpty(hotkey)) return;
            UnregisterHotKey(_hwnd, id);
            ParseHotkey(hotkey, out uint mods, out uint vk);
            if (vk != 0) RegisterHotKey(_hwnd, id, mods, vk);
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam,
                                       ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = (int)wParam;

                if (_idToAction.TryGetValue(id, out var action))
                {
                    HotkeyFired?.Invoke(action);
                    handled = true;
                }
                else if (_idToMacroFile.TryGetValue(id, out var filePath))
                {
                    MacroHotkeyFired?.Invoke(filePath);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
    }
}
