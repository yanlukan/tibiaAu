using System.Drawing;
using System.Drawing.Imaging;

namespace UiApp;

internal static class CaptureHelper
{
    internal static Bitmap CaptureRect(Rectangle rectOnScreen)
    {
        var bmp = new Bitmap(rectOnScreen.Width, rectOnScreen.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(rectOnScreen.Location, Point.Empty, rectOnScreen.Size);
        }
        return bmp;
    }
}
