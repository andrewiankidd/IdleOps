# imgfnd

Windows-only CLI that finds a reference image within a window screenshot using OpenCV template matching. Outputs center coordinates as `x,y` on stdout.

- **Docs**: [docs/imgfnd/](../../docs/imgfnd/README.md)
- **Build**: `dotnet build src/imgfnd/imgfnd.csproj`
- **Test**: `dotnet test src/imgfnd.Tests/imgfnd.Tests.csproj`
- **Dependencies**: shared, OpenCvSharp4, OpenCvSharp4.runtime.win
- **Platforms**: Windows only
