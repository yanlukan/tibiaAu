# tibiaAu (Windows OCR automation starter)

This repo is a minimal starter for:
- Capturing a **Windows** game window to PNG frames (C#)
- Running OCR on saved frames (Python + Tesseract)

It’s designed so you can **edit on a Mac** and **run/test on a Windows machine**.

## Recommended setup (M1 Pro Mac)

Because low-level graphics/capture behavior differs on Windows-on-ARM, the most reliable workflow is:

1. Use a **Windows 11 x64** test box (local PC or cloud VM with GPU)
2. Remote into it (Parsec/Moonlight/RDP)
3. Clone this repo on that Windows box and run the tools there

## 1) Capture frames (Windows)

### Prereqs
- .NET 8 SDK

### Build
From `windows/`:

```powershell
cd windows

dotnet build .\CaptureFrames.sln
```

### Run
Capture by window title substring:

```powershell
dotnet run --project .\CaptureFrames\CaptureFrames.csproj -- --window-title "miracle 7.4" --out .\..\frames --fps 2
```

Capture a specific UI region within the client area (`x,y,w,h`):

```powershell
dotnet run --project .\CaptureFrames\CaptureFrames.csproj -- --window-title "miracle 7.4" --out .\..\frames --fps 2 --region 10,10,300,80
```

Output:
- `frames/latest.png` always points to the newest frame
- `frames/frame_*.png` is an archive of frames

## 2) OCR (Windows)

### Prereqs
- Python 3.11+ recommended
- Tesseract OCR engine installed
  - via `winget` (example):

```powershell
winget install --id UB-Mannheim.TesseractOCR
```

### Install Python deps
From repo root:

```powershell
python -m venv .venv
# If PowerShell blocks scripts, run this once per session:
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

.\.venv\Scripts\Activate.ps1
pip install -r .\python\ocr\requirements.txt
```

### Run OCR on the latest frame
```powershell
python .\python\ocr\ocr.py --image .\frames\latest.png
```

If you captured a full frame but want OCR on only a UI area:

```powershell
python .\python\ocr\ocr.py --image .\frames\latest.png --roi 10,10,300,80 --psm 7
```

Tip: write the preprocessed image to inspect OCR quality:

```powershell
python .\python\ocr\ocr.py --image .\frames\latest.png --roi 10,10,300,80 --debug-out .\frames\debug_pre.png
```

## Notes
- The C# capture uses screen-based capture (`CopyFromScreen`), so the window must be visible on the Windows desktop.
- For best results, run your game in **windowed or borderless-windowed** mode for capture.
