using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace UiApp;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;
    private Bitmap? _lastBitmap;

    public MainWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => CaptureAndPreview();
        StatusText.Text = "Idle.";
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
            var title = WindowTitleText.Text?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                StatusText.Text = "Enter a window title substring.";
                return;
            }

            var hwnd = Win32.FindWindowByTitleSubstring(title);
            if (hwnd == IntPtr.Zero)
            {
                StatusText.Text = "Window not found.";
                return;
            }

            var client = Win32.GetClientRectOnScreen(hwnd);
            if (client.Width <= 0 || client.Height <= 0)
            {
                StatusText.Text = "Client rect invalid.";
                return;
            }

            var rect = client;
            var regionText = RegionText.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(regionText))
            {
                if (!TryParseRegion(regionText, out var r))
                {
                    StatusText.Text = "Region format: x,y,w,h";
                    return;
                }

                rect = new Rectangle(client.X + r.X, client.Y + r.Y, r.Width, r.Height);
            }

            _lastBitmap?.Dispose();
            _lastBitmap = CaptureHelper.CaptureRect(rect);

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
