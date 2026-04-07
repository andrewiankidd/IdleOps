# shared

Shared class library providing cross-cutting utilities for all IdleOps tools.

- **Build**: `dotnet build src/shared/shared.csproj`
- **Test**: `dotnet test src/shared.Tests/shared.Tests.csproj`
- **Dependencies**: System.Drawing.Common
- **Consumers**: all tools except spkbak and cnvrtr

Key namespaces: `IdleOps.Shared.Platform`, `IdleOps.Shared.Logging`, `IdleOps.Shared.Cli`, `IdleOps.Shared.Capture`, `IdleOps.Shared.Windows`
