using System;
using System.Runtime.InteropServices;

namespace MiniMacro
{
    // Screen coordinates from MSLLHOOKSTRUCT and GetCursorPos must be compared
    // with Win32 virtual-desktop metrics, not DPI-adjusted WPF SystemParameters.
    internal readonly struct VirtualDesktop
    {
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        public int Left { get; }
        public int Top { get; }
        public int Width { get; }
        public int Height { get; }

        private VirtualDesktop(int left, int top, int width, int height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public static VirtualDesktop Capture()
        {
            int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
            if (width <= 0 || height <= 0)
                throw new InvalidOperationException("Не удалось определить размеры виртуального рабочего стола.");
            return new VirtualDesktop(left, top, width, height);
        }

        // Both ends are pixel centers: the last valid pixel is at Width - 1.
        public double NormalizeX(int x) => Width > 1 ? (double)(x - Left) / (Width - 1) : 0;
        public double NormalizeY(int y) => Height > 1 ? (double)(y - Top) / (Height - 1) : 0;

        public int PixelX(double x) => Left + (int)Math.Round(Clamp01(x) * (Width - 1));
        public int PixelY(double y) => Top + (int)Math.Round(Clamp01(y) * (Height - 1));

        // MOUSEEVENTF_VIRTUALDESK maps 0..65535 onto the full virtual desktop.
        public static int Absolute(double normalized) =>
            (int)Math.Round(Clamp01(normalized) * 65535.0);

        private static double Clamp01(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Некорректная координата макроса.");
            return Math.Max(0.0, Math.Min(1.0, value));
        }
    }
}
