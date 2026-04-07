# waitfr

Windows-only CLI that polls until a window appears (or disappears), optionally waiting for specific text via OCR. Like Playwright's `waitForSelector` but for the desktop.

- **Docs**: [docs/waitfr/](../../docs/waitfr/README.md)
- **Build**: `dotnet build src/waitfr/waitfr.csproj`
- **Test**: `dotnet test src/waitfr.Tests/waitfr.Tests.csproj`
- **Dependencies**: shared
- **TFM**: `net10.0-windows10.0.22621.0` (WinRT OCR APIs for --text mode)
- **Platforms**: Windows 10/11
