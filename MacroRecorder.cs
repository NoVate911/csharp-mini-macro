using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace MiniMacro
{
    internal sealed class MacroRecorder
    {
        // ── P/Invoke ─────────────────────────────────────────────────────────

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Структуры Windows API
        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT  pt;
            public uint   mouseData;
            public uint   flags;
            public uint   time;
            public UIntPtr dwExtraInfo;
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL    = 14;

        private const int WM_KEYDOWN    = 0x0100;
        private const int WM_KEYUP      = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP   = 0x0105;

        private const int WM_MOUSEMOVE   = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP   = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP   = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP   = 0x0208;
        private const int WM_MOUSEWHEEL  = 0x020A;

        // ── Поля ─────────────────────────────────────────────────────────────

        // Держим делегаты как поля, иначе GC их уберёт
        private readonly LowLevelProc _keyProc;
        private readonly LowLevelProc _mouseProc;

        private IntPtr _keyHook   = IntPtr.Zero;
        private IntPtr _mouseHook = IntPtr.Zero;

        private readonly List<MacroAction> _actions = new List<MacroAction>();
        private readonly Stopwatch         _sw      = new Stopwatch();

        private long _lastMouseMoveTs = -1;
        private const long MouseMoveThresholdMs = 16;

        public bool IsRecording { get; private set; }

        public MacroRecorder()
        {
            _keyProc   = KeyboardProc;
            _mouseProc = MouseProc;
        }

        // ── Управление ───────────────────────────────────────────────────────

        public void Start()
        {
            _actions.Clear();
            _lastMouseMoveTs = -1;
            _sw.Restart();
            IsRecording = true;

            using var proc = Process.GetCurrentProcess();
            using var mod  = proc.MainModule!;
            var hMod = GetModuleHandle(mod.ModuleName!);

            _keyHook   = SetWindowsHookEx(WH_KEYBOARD_LL, _keyProc,   hMod, 0);
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL,    _mouseProc, hMod, 0);
        }

        public List<MacroAction> Stop()
        {
            IsRecording = false;
            _sw.Stop();

            if (_keyHook   != IntPtr.Zero) { UnhookWindowsHookEx(_keyHook);   _keyHook   = IntPtr.Zero; }
            if (_mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(_mouseHook); _mouseHook = IntPtr.Zero; }

            return new List<MacroAction>(_actions);
        }

        // ── Хук клавиатуры ───────────────────────────────────────────────────

        private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var ks  = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                var msg = (int)wParam;

                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                    Record(new MacroAction
                    {
                        Type       = MacroActionType.KeyDown,
                        Timestamp  = _sw.ElapsedMilliseconds,
                        VirtualKey = (int)ks.vkCode,
                        ScanCode   = (int)ks.scanCode
                    });
                else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                    Record(new MacroAction
                    {
                        Type       = MacroActionType.KeyUp,
                        Timestamp  = _sw.ElapsedMilliseconds,
                        VirtualKey = (int)ks.vkCode,
                        ScanCode   = (int)ks.scanCode
                    });
            }
            return CallNextHookEx(_keyHook, nCode, wParam, lParam);
        }

        // ── Хук мыши ────────────────────────────────────────────────────────

        private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var ms  = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var msg = (int)wParam;
                var ts  = _sw.ElapsedMilliseconds;

                double normX = ms.pt.x / SystemParameters.PrimaryScreenWidth;
                double normY = ms.pt.y / SystemParameters.PrimaryScreenHeight;

                switch (msg)
                {
                    case WM_MOUSEMOVE:
                        if (ts - _lastMouseMoveTs >= MouseMoveThresholdMs)
                        {
                            _lastMouseMoveTs = ts;
                            Record(new MacroAction
                            {
                                Type      = MacroActionType.MouseMove,
                                Timestamp = ts,
                                NormX     = normX,
                                NormY     = normY
                            });
                        }
                        break;

                    case WM_LBUTTONDOWN:
                        Record(new MacroAction { Type = MacroActionType.MouseDown, Timestamp = ts, NormX = normX, NormY = normY, Button = "Left" });
                        break;
                    case WM_LBUTTONUP:
                        Record(new MacroAction { Type = MacroActionType.MouseUp,   Timestamp = ts, NormX = normX, NormY = normY, Button = "Left" });
                        break;
                    case WM_RBUTTONDOWN:
                        Record(new MacroAction { Type = MacroActionType.MouseDown, Timestamp = ts, NormX = normX, NormY = normY, Button = "Right" });
                        break;
                    case WM_RBUTTONUP:
                        Record(new MacroAction { Type = MacroActionType.MouseUp,   Timestamp = ts, NormX = normX, NormY = normY, Button = "Right" });
                        break;
                    case WM_MBUTTONDOWN:
                        Record(new MacroAction { Type = MacroActionType.MouseDown, Timestamp = ts, NormX = normX, NormY = normY, Button = "Middle" });
                        break;
                    case WM_MBUTTONUP:
                        Record(new MacroAction { Type = MacroActionType.MouseUp,   Timestamp = ts, NormX = normX, NormY = normY, Button = "Middle" });
                        break;

                    case WM_MOUSEWHEEL:
                        int delta = (short)((ms.mouseData >> 16) & 0xFFFF);
                        Record(new MacroAction { Type = MacroActionType.MouseWheel, Timestamp = ts, Delta = delta });
                        break;
                }
            }
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private void Record(MacroAction action) => _actions.Add(action);
    }
}
