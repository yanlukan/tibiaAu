using System.Drawing;
using System.Drawing.Imaging;

namespace UiApp;

internal static class CaptureHelper
{
    internal static Bitmap CaptureClient(IntPtr hwnd, Rectangle clientRect)
    {
        // Prefer PrintWindow so capture works even if the window is occluded.
        var bmp = new Bitmap(clientRect.Width, clientRect.Height, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        var hdc = g.GetHdc();
        try
        {
            if (Win32.PrintWindowClient(hwnd, hdc))
                return bmp;
        }
        finally
        {
            g.ReleaseHdc(hdc);
        }

        // Fallback: screen-based capture (requires window to be visible).
        var rectOnScreen = Win32.GetClientRectOnScreen(hwnd);
        using (var g2 = Graphics.FromImage(bmp))
        {
            g2.CopyFromScreen(rectOnScreen.Location, Point.Empty, rectOnScreen.Size);
        }
        return bmp;
    }
}
