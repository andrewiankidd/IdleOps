namespace IdleOps.Shared.Windows;

public sealed record WindowInfo(IntPtr Handle, string Title, uint ProcessId);
