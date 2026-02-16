using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace UiApp;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private Bitmap? _lastBitmap;
    private IntPtr _attachedHwnd;

    private const int HotkeyIdAttach = 1;
    private const int WM_HOTKEY = 0x0312;
    private const int VK_F5 = 0x74;
    private HwndSource? _hwndSource;

    public MainWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => CaptureAndPreview();
        StatusText.Text = "Idle.";

        Loaded += (_, _) => RegisterHotkeys();
        Closed += (_, _) => UnregisterHotkeys();
    }

    private void RegisterHotkeys()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(hwnd);
        _hwndSource.AddHook(WndProc);

        // Global F5: attach to the currently focused (foreground) window.
        if (!Win32.RegisterHotKey(hwnd, HotkeyIdAttach, 0 /* no modifiers */, VK_F5))
        {
            StatusText.Text = "Could not register global F5 hotkey.";
        }
        else
        {
            StatusText.Text = "Tip: focus the game and press F5 to attach.";
        }
    }

    private void UnregisterHotkeys()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            Win32.UnregisterHotKey(hwnd, HotkeyIdAttach);
        }
        catch
        {
            // best-effort
        }

        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyIdAttach)
        {
            handled = true;
            AttachToForegroundWindow();
        }

        return IntPtr.Zero;
    }

    private void AttachToForegroundWindow()
    {
        var fg = Win32.GetForegroundWindow();
        if (fg == IntPtr.Zero)
        {
            StatusText.Text = "No foreground window.";
            return;
        }

        _attachedHwnd = fg;
        var title = Win32.GetWindowTitle(fg);
        if (!string.IsNullOrWhiteSpace(title))
            WindowTitleText.Text = title;

        StatusText.Text = $"Attached to: {title}";
    }

    private void StartStop_Click(object sender, RoutedEventArgs e)
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
            StartStopButton.Content = "Start";
            StatusText.Text = "Stopped.";
            return;
        }

        if (!TryGetFps(out var fps))
        {
            MessageBox.Show(this, "Invalid FPS.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _timer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, Math.Round(1000.0 / fps)));
        _timer.Start();
        StartStopButton.Content = "Stop";
        StatusText.Text = "Capturing…";
        CaptureAndPreview();
    }

    private void CaptureOnce_Click(object sender, RoutedEventArgs e)
    {
        CaptureAndPreview();
    }

    private async void Ocr_Click(object sender, RoutedEventArgs e)
    {
        if (_lastBitmap is null)
        {
            StatusText.Text = "No frame captured yet.";
            return;
        }

        try
        {
            StatusText.Text = "Running OCR…";
            var text = await OcrRunner.RecognizeAsync(_lastBitmap);
            OcrOutput.Text = text.Trim();
            StatusText.Text = "OCR complete.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "OCR failed.";
            MessageBox.Show(this, ex.Message, "OCR error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CaptureAndPreview()
    {
        try
        {
            var hwnd = _attachedHwnd;
            if (hwnd == IntPtr.Zero)
            {
                var title = WindowTitleText.Text?.Trim();
                if (string.IsNullOrWhiteSpace(title))
                {
                    StatusText.Text = "Enter a window title substring (or press F5 while game is focused).";
                    return;
                }

                hwnd = Win32.FindWindowByTitleSubstring(title);
                if (hwnd == IntPtr.Zero)
                {
                    StatusText.Text = "Window not found.";
                    return;
                }
            }

            var rect = Win32.GetClientRect(hwnd);
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                StatusText.Text = "Client rect invalid.";
                return;
            }
            var regionText = RegionText.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(regionText))
            {
                if (!TryParseRegion(regionText, out var r))
                {
                    StatusText.Text = "Region format: x,y,w,h";
                    return;
                }

                rect = new Rectangle(r.X, r.Y, r.Width, r.Height);
            }

            _lastBitmap?.Dispose();
            _lastBitmap = CaptureHelper.CaptureClient(hwnd, rect);

            PreviewImage.Source = ToBitmapSource(_lastBitmap);
            StatusText.Text = $"Captured {rect.Width}x{rect.Height} @ {DateTime.Now:T}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Capture failed.";
            MessageBox.Show(this, ex.Message, "Capture error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static BitmapSource ToBitmapSource(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private bool TryGetFps(out double fps)
    {
        return double.TryParse(FpsText.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out fps);
    }

    private static bool TryParseRegion(string text, out Rectangle rect)
    {
        rect = default;
        var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4)
            return false;
        if (!int.TryParse(parts[0], out var x)) return false;
        if (!int.TryParse(parts[1], out var y)) return false;
        if (!int.TryParse(parts[2], out var w)) return false;
        if (!int.TryParse(parts[3], out var h)) return false;
        rect = new Rectangle(x, y, w, h);
        return w > 0 && h > 0;
    }
}
