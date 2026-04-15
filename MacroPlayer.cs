using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MiniMacro
{
    internal sealed class MacroPlayer
    {
        // ── P/Invoke ─────────────────────────────────────────────────────────

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint   type;
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT    mi;
            [FieldOffset(0)] public KEYBDINPUT    ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int    dx, dy;
            public uint   mouseData;
            public uint   dwFlags;
            public uint   time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint   dwFlags;
            public uint   time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        private const uint INPUT_MOUSE    = 0;
        private const uint INPUT_KEYBOARD = 1;

        private const uint KEYEVENTF_SCANCODE = 0x0008;
        private const uint KEYEVENTF_KEYUP    = 0x0002;

        private const uint MOUSEEVENTF_MOVE        = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN    = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP      = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN   = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP     = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN  = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP    = 0x0040;
        private const uint MOUSEEVENTF_WHEEL       = 0x0800;
        private const uint MOUSEEVENTF_ABSOLUTE    = 0x8000;
        // Флаг: координаты отображаются на весь виртуальный рабочий стол (все мониторы)
        private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;

        // Порог рывка мыши: если курсор отклонился от последней позиции макроса
        // более чем на JerkThresholdPx пикселей — считаем это намеренным движением и останавливаем
        private const int JerkThresholdPx = 150;

        // ── Поля ─────────────────────────────────────────────────────────────

        private ManualResetEventSlim    _pauseEvent = new ManualResetEventSlim(true);
        private CancellationTokenSource? _cts;

        // Последняя позиция мыши, выставленная макросом (в экранных пикселях виртуального стола)
        // int.MinValue = мышь ещё не перемещалась макросом
        private volatile int _lastSentX = int.MinValue;
        private volatile int _lastSentY = int.MinValue;

        public event Action? PlaybackCompleted;

        // ── Управление ───────────────────────────────────────────────────────

        public Task Play(List<MacroAction> actions, double speed, int repeatCount)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _pauseEvent.Set();
            _lastSentX = int.MinValue;
            _lastSentY = int.MinValue;

            return Task.Run(async () =>
            {
                // Запускаем мониторинг рывка параллельно с воспроизведением
                var jerkTask = MonitorJerkAsync(token);

                try
                {
                    for (int rep = 0; rep < repeatCount && !token.IsCancellationRequested; rep++)
                    {
                        long prevTs = 0;
                        foreach (var action in actions)
                        {
                            token.ThrowIfCancellationRequested();
                            _pauseEvent.Wait(token);

                            long delay = (long)((action.Timestamp - prevTs) / speed);
                            if (delay > 0)
                                await Task.Delay((int)Math.Min(delay, int.MaxValue), token);

                            prevTs = action.Timestamp;
                            SendAction(action);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    await jerkTask;
                    PlaybackCompleted?.Invoke();
                }
            }, token);
        }

        public void Pause()  => _pauseEvent.Reset();
        public void Resume() => _pauseEvent.Set();

        public void Stop()
        {
            _cts?.Cancel();
            _pauseEvent.Set(); // разблокировать ожидание паузы
        }

        // ── Обнаружение рывка мыши ───────────────────────────────────────────

        private async Task MonitorJerkAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(35, token); }
                catch (OperationCanceledException) { return; }

                int expectedX = _lastSentX;
                int expectedY = _lastSentY;

                // Нет мышиных событий ещё или воспроизведение приостановлено — не проверяем
                if (expectedX == int.MinValue || !_pauseEvent.IsSet) continue;

                if (!GetCursorPos(out POINT actual)) continue;

                long dx = actual.X - expectedX;
                long dy = actual.Y - expectedY;
                if (dx * dx + dy * dy > (long)JerkThresholdPx * JerkThresholdPx)
                {
                    // Пользователь резко сдвинул мышь — останавливаем макрос
                    _cts?.Cancel();
                    return;
                }
            }
        }

        // ── Отправка событий ─────────────────────────────────────────────────

        private void SendAction(MacroAction action)
        {
            switch (action.Type)
            {
                case MacroActionType.KeyDown:
                    SendKey((ushort)action.ScanCode, false);
                    break;
                case MacroActionType.KeyUp:
                    SendKey((ushort)action.ScanCode, true);
                    break;
                case MacroActionType.MouseMove:
                    SendMouseMove(action.NormX, action.NormY);
                    break;
                case MacroActionType.MouseDown:
                    SendMouseButton(action.Button, true,  action.NormX, action.NormY);
                    break;
                case MacroActionType.MouseUp:
                    SendMouseButton(action.Button, false, action.NormX, action.NormY);
                    break;
                case MacroActionType.MouseWheel:
                    SendWheel(action.Delta);
                    break;
            }
        }

        private static void SendKey(ushort scan, bool keyUp)
        {
            var input = new INPUT { type = INPUT_KEYBOARD };
            input.u.ki.wScan   = scan;
            input.u.ki.dwFlags = KEYEVENTF_SCANCODE | (keyUp ? KEYEVENTF_KEYUP : 0);
            SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
        }

        private void SendMouseMove(double normX, double normY)
        {
            var (absX, absY) = ToVirtualDesktopCoords(normX, normY);

            var input = new INPUT { type = INPUT_MOUSE };
            input.u.mi.dx      = absX;
            input.u.mi.dy      = absY;
            input.u.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK | MOUSEEVENTF_MOVE;
            SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());

            UpdateLastSent(normX, normY);
        }

        private void SendMouseButton(string? button, bool down, double normX, double normY)
        {
            uint flags = button switch
            {
                "Right"  => down ? MOUSEEVENTF_RIGHTDOWN  : MOUSEEVENTF_RIGHTUP,
                "Middle" => down ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP,
                _        => down ? MOUSEEVENTF_LEFTDOWN   : MOUSEEVENTF_LEFTUP
            };

            var (absX, absY) = ToVirtualDesktopCoords(normX, normY);

            // Переместить в нужную позицию + нажать
            var move  = new INPUT { type = INPUT_MOUSE };
            move.u.mi.dx      = absX;
            move.u.mi.dy      = absY;
            move.u.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK | MOUSEEVENTF_MOVE;

            var click = new INPUT { type = INPUT_MOUSE };
            click.u.mi.dwFlags = flags;

            SendInput(2, new[] { move, click }, Marshal.SizeOf<INPUT>());

            UpdateLastSent(normX, normY);
        }

        private static void SendWheel(int delta)
        {
            var input = new INPUT { type = INPUT_MOUSE };
            input.u.mi.mouseData = (uint)delta;
            input.u.mi.dwFlags   = MOUSEEVENTF_WHEEL;
            SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
        }

        // ── Вспомогательные методы ───────────────────────────────────────────

        // Переводит нормализованные координаты (0..1 от виртуального стола) в
        // абсолютные координаты SendInput (0..65535), пригодные для MOUSEEVENTF_VIRTUALDESK
        private static (int absX, int absY) ToVirtualDesktopCoords(double normX, double normY)
        {
            int absX = (int)(normX * 65535);
            int absY = (int)(normY * 65535);
            return (absX, absY);
        }

        // Запоминаем физическую позицию последнего посланного мышиного события
        // для обнаружения рывка пользователя
        private void UpdateLastSent(double normX, double normY)
        {
            _lastSentX = (int)(normX * SystemParameters.VirtualScreenWidth  + SystemParameters.VirtualScreenLeft);
            _lastSentY = (int)(normY * SystemParameters.VirtualScreenHeight + SystemParameters.VirtualScreenTop);
        }
    }
}
