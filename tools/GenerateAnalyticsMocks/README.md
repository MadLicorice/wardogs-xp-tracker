# Generate analytics mocks

This development tool creates 12 PNG fixtures and an isolated season with example sessions. The images use the actual OCR geometry. No game screenshot or player save is captured.

Use Windows and the .NET 10 SDK:

```powershell
dotnet run --project tools/GenerateAnalyticsMocks -- --output 'C:\path\to\empty-folder' --verify
```

The output folder must be empty. `--verify` reads every PNG through `XpImageReader` and checks the exact expected values. Its JSON report includes reader diagnostics.

`DemoStatsFactory.CreateAnalyticsSeason()` returns the sample season without file I/O. The generated Markdown save belongs to the mock pack. Keep it separate from personal save files.

These fixtures exercise parser behavior. They do not establish support for new game resolutions, layouts, or display modes.

Generated PNGs retain only image structure, transparency, and pixel data. The encoder omits optional color, resolution, text, and EXIF chunks.
