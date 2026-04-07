namespace IdleOps.Shared.Cli;

public static class HelpPrinter
{
    public static void Print(HelpContent content)
    {
        Console.WriteLine($"{content.Name}");
        Console.WriteLine(content.Description);
        Console.WriteLine();
        Console.WriteLine("Usage:");
        foreach (var line in content.UsageExamples)
        {
            Console.WriteLine($"  {line}");
        }
        Console.WriteLine();
        Console.WriteLine("Options:");
        foreach (var option in content.Options)
        {
            Console.WriteLine($"  {option}");
        }
    }

    public static void PrintVersion(string name, string version)
    {
        Console.WriteLine($"{name} {version}");
    }
}
