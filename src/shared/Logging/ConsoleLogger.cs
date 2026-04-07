namespace IdleOps.Shared.Logging;

public static class ConsoleLogger
{
    public static void Info(string message) => Console.WriteLine(message);
    public static void Warn(string message) => Console.WriteLine($"[warn] {message}");
    public static void Error(string message) => Console.Error.WriteLine(message);
}
