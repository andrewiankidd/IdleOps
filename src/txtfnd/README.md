# txtfnd

Windows-only CLI that screenshots a window, runs OCR via `Windows.Media.Ocr`, finds target text, and outputs its center coordinates as `x,y` on stdout. Designed for piping into inpctl.

- **Docs**: [docs/txtfnd/](../../docs/txtfnd/README.md)
- **Build**: `dotnet build src/txtfnd/txtfnd.csproj`
- **Test**: `dotnet test src/txtfnd.Tests/txtfnd.Tests.csproj`
- **Dependencies**: shared
- **TFM**: `net10.0-windows10.0.22621.0` (WinRT OCR APIs)
- **Platforms**: Windows 10/11 with OCR language pack
