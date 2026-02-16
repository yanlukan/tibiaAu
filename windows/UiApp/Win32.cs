using System.Drawing;
using System.Runtime.InteropServices;

namespace UiApp;

internal static class Win32
{
    internal static IntPtr GetForegroundWindow() => GetForegroundWindowNative();

    internal static string GetWindowTitle(IntPtr hWnd)
    {
        var length = GetWindowTextLengthW(hWnd);
        if (length <= 0)
            return string.Empty;
        var sb = new System.Text.StringBuilder(length + 1);
        GetWindowTextW(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

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

    internal static Rectangle GetClientRect(IntPtr hWnd)
    {
        if (!GetClientRectNative(hWnd, out var r))
            return Rectangle.Empty;
        return new Rectangle(0, 0, r.Right - r.Left, r.Bottom - r.Top);
    }

    internal static Rectangle GetClientRectOnScreen(IntPtr hWnd)
    {
        if (!GetClientRectNative(hWnd, out var clientRect))
            return Rectangle.Empty;

        var topLeft = new POINT { X = clientRect.Left, Y = clientRect.Top };
        if (!ClientToScreen(hWnd, ref topLeft))
            return Rectangle.Empty;

        var width = clientRect.Right - clientRect.Left;
        var height = clientRect.Bottom - clientRect.Top;

        return new Rectangle(topLeft.X, topLeft.Y, width, height);
    }

    internal static bool PrintWindowClient(IntPtr hWnd, IntPtr targetHdc)
    {
        // Ask the window to render into our HDC.
        // 2 = PW_RENDERFULLCONTENT (best effort)
        if (!PrintWindow(hWnd, targetHdc, 2))
            return false;

        // Note: PrintWindow renders the full window for many apps; for many 2D games
        // it still works well enough for OCR even when occluded.
        return true;
    }

    internal static bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, int vk)
        => RegisterHotKeyNative(hWnd, id, modifiers, vk);

    internal static bool UnregisterHotKey(IntPtr hWnd, int id)
        => UnregisterHotKeyNative(hWnd, id);

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
    private static extern bool GetClientRectNative(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindowNative();

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKeyNative(IntPtr hWnd, int id, uint fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKeyNative(IntPtr hWnd, int id);

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
