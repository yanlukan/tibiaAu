using System.Drawing;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace UiApp;

internal static class OcrRunner
{
    internal static async Task<string> RecognizeAsync(Bitmap bitmap)
    {
        // Uses Windows built-in OCR engine. Requires language pack installed.
        var engine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null)
            throw new InvalidOperationException("Windows OCR engine not available. Install a Windows language pack (e.g., English).");

        var softwareBitmap = await ToSoftwareBitmapAsync(bitmap);
        var result = await engine.RecognizeAsync(softwareBitmap);
        return result.Text ?? string.Empty;
    }

    private static async Task<SoftwareBitmap> ToSoftwareBitmapAsync(Bitmap bitmap)
    {
        byte[] pngBytes;
        using (var ms = new MemoryStream())
        {
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            pngBytes = ms.ToArray();
        }

        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream))
        {
            writer.WriteBytes(pngBytes);
            await writer.StoreAsync();
            await writer.FlushAsync();
        }

        stream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    }
}
