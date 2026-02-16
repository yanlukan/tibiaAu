using System.CommandLine;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace CaptureFrames;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private static int Main(string[] args)
    {
        var windowTitleOption = new Option<string>(
            name: "--window-title",
            description: "Substring of the window title to capture.")
        { IsRequired = true };

        var outDirOption = new Option<DirectoryInfo>(
            name: "--out",
            description: "Output directory for PNG frames.")
        { IsRequired = true };

        var fpsOption = new Option<double>(
            name: "--fps",
            getDefaultValue: () => 2,
            description: "Frames per second to capture.");

        var regionOption = new Option<string?>(
            name: "--region",
            description: "Optional region within client area: x,y,w,h (pixels). Example: 10,10,200,50");

        var onceOption = new Option<bool>(
            name: "--once",
            description: "Capture a single frame and exit.");

        var root = new RootCommand("Capture a window's client area to PNG frames (Windows only).")
        {
            windowTitleOption,
            outDirOption,
            fpsOption,
            regionOption,
            onceOption,
        };

        root.SetHandler((string windowTitle, DirectoryInfo outDir, double fps, string? region, bool once) =>
        {
            Run(windowTitle, outDir, fps, region, once);
        }, windowTitleOption, outDirOption, fpsOption, regionOption, onceOption);

        return root.Invoke(args);
    }

    private static void Run(string windowTitle, DirectoryInfo outDir, double fps, string? region, bool once)
    {
        outDir.Create();

        var windowHandle = Win32.FindWindowByTitleSubstring(windowTitle);
        if (windowHandle == IntPtr.Zero)
        {
            Console.Error.WriteLine($"Could not find a window whose title contains: '{windowTitle}'");
            Environment.Exit(2);
            return;
        }

        var fullClientRectOnScreen = Win32.GetClientRectOnScreen(windowHandle);
        var captureRect = fullClientRectOnScreen;

        if (!string.IsNullOrWhiteSpace(region))
        {
            var r = ParseRegion(region);
            captureRect = new Rectangle(
                x: fullClientRectOnScreen.X + r.X,
                y: fullClientRectOnScreen.Y + r.Y,
                width: r.Width,
                height: r.Height);
        }

        if (captureRect.Width <= 0 || captureRect.Height <= 0)
        {
            Console.Error.WriteLine("Capture rectangle has non-positive size.");
            Environment.Exit(3);
            return;
        }

        var intervalMs = fps <= 0 ? 500 : (int)Math.Max(1, Math.Round(1000.0 / fps));
        var sw = Stopwatch.StartNew();

        int frameIndex = 0;
        do
        {
            var framePath = Path.Combine(outDir.FullName, $"frame_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{frameIndex:D6}.png");
            var latestPath = Path.Combine(outDir.FullName, "latest.png");

            CaptureToPng(captureRect, framePath);

            try
            {
                File.Copy(framePath, latestPath, overwrite: true);
            }
            catch
            {
                // best-effort
            }

            frameIndex++;

            if (once)
                break;

            var target = frameIndex * intervalMs;
            var delay = target - sw.ElapsedMilliseconds;
            if (delay > 0)
                Thread.Sleep((int)delay);
        } while (true);
    }

    private static Rectangle ParseRegion(string region)
    {
        var parts = region.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || !int.TryParse(parts[0], out var x) || !int.TryParse(parts[1], out var y) ||
            !int.TryParse(parts[2], out var w) || !int.TryParse(parts[3], out var h))
        {
            throw new ArgumentException("Invalid --region format. Expected x,y,w,h");
        }

        return new Rectangle(x, y, w, h);
    }

    private static void CaptureToPng(Rectangle rectOnScreen, string path)
    {
        using var bmp = new Bitmap(rectOnScreen.Width, rectOnScreen.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(rectOnScreen.Location, Point.Empty, rectOnScreen.Size);
        }

        bmp.Save(path, ImageFormat.Png);
    }
}
