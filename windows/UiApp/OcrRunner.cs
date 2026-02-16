using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace UiApp;

internal static class OcrRunner
{
    internal static async Task<string> RecognizeAsync(Bitmap bitmap)
    {
        var scriptPath = FindRepoScriptPath("python", "ocr", "ocr.py");
        var tempDir = Path.Combine(Path.GetTempPath(), "tibiaAu");
        Directory.CreateDirectory(tempDir);
        var imagePath = Path.Combine(tempDir, "ui_latest.png");

        bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);

        // Run python OCR script and parse JSON output
        List<string> args = new() { scriptPath, "--image", imagePath, "--json" };
        var (exitCode, stdout, stderr) = await RunProcessAsync("python", args);
        if (exitCode != 0)
        {
            var msg = new StringBuilder();
            msg.AppendLine("OCR process failed.");
            msg.AppendLine($"Exit code: {exitCode}");
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                msg.AppendLine("--- stderr ---");
                msg.AppendLine(stderr.Trim());
            }
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                msg.AppendLine("--- stdout ---");
                msg.AppendLine(stdout.Trim());
            }
            throw new InvalidOperationException(msg.ToString());
        }

        using var doc = JsonDocument.Parse(stdout);
        if (doc.RootElement.TryGetProperty("text", out var textProp))
            return textProp.GetString() ?? string.Empty;

        return stdout.Trim();
    }

    private static string FindRepoScriptPath(params string[] parts)
    {
        // Walk upward from the app base directory to find <repoRoot>\python\ocr\ocr.py
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && dir is not null; i++)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate OCR script: {Path.Combine(parts)}. Run the app from within the repo folder.");
    }

    private static async Task<(int exitCode, string stdout, string stderr)> RunProcessAsync(string fileName, IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (var a in args)
            psi.ArgumentList.Add(a);

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start process: {fileName}");
        var stdoutTask = p.StandardOutput.ReadToEndAsync();
        var stderrTask = p.StandardError.ReadToEndAsync();

        await p.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return (p.ExitCode, stdout, stderr);
    }
}
