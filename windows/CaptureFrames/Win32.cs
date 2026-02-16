using System.Drawing;
using System.Runtime.InteropServices;

namespace CaptureFrames;

internal static class Win32
{
    internal static IntPtr FindWindowByTitleSubstring(string titleSubstring)
    {
        titleSubstring = titleSubstring ?? string.Empty;

        IntPtr found = IntPtr.Zero;
        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            var length = GetWindowTextLengthW(hWnd);
            if (length <= 0)
                return true;

            var sb = new System.Text.StringBuilder(length + 1);
            GetWindowTextW(hWnd, sb, sb.Capacity);
            var title = sb.ToString();

            if (title.Contains(titleSubstring, StringComparison.OrdinalIgnoreCase))
            {
                found = hWnd;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        return found;
    }

    internal static Rectangle GetClientRectOnScreen(IntPtr hWnd)
    {
        if (!GetClientRect(hWnd, out var clientRect))
            return Rectangle.Empty;

        var topLeft = new POINT { X = clientRect.Left, Y = clientRect.Top };
        if (!ClientToScreen(hWnd, ref topLeft))
            return Rectangle.Empty;

        var width = clientRect.Right - clientRect.Left;
        var height = clientRect.Bottom - clientRect.Top;

        return new Rectangle(topLeft.X, topLeft.Y, width, height);
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextW(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLengthW(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
