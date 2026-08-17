using System.Diagnostics;

namespace IdleOps.Shared.Capture;

/// <summary>Minimal external-process helper shared by the Linux/macOS capturers.</summary>
internal static class ProcessRunner
{
    public static (bool ok, string stdout, string stderr) Run(string file, params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var p = Process.Start(psi);
            if (p is null) return (false, "", "");
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return (p.ExitCode == 0, stdout, stderr);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[scrcap] failed to run {file}: {ex.Message}");
            return (false, "", "");
        }
    }

    public static bool ToolExists(string tool)
    {
        try { return Run("which", tool).ok; }
        catch { return false; }
    }
}
