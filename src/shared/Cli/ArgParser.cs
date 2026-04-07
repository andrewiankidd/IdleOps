namespace IdleOps.Shared.Cli;

/// <summary>
/// Lightweight shared argument parser. Eliminates per-tool switch/case boilerplate.
///
/// Usage:
///   var p = new ArgParser(args);
///   p.On("-w", "--window", v => window = v);
///   p.On("-t", "--timer", v => timer = ParseDouble(v), "timer");
///   p.Flag("-h", "--help", () => showHelp = true);
///   p.Parse(); // throws ArgumentException on unknown args or missing values
/// </summary>
public sealed class ArgParser
{
    private readonly string[] _args;
    private readonly List<ArgDef> _defs = [];

    public ArgParser(string[] args)
    {
        _args = args;
    }

    /// <summary>Register an option that takes a value (e.g., -w "Notepad*").</summary>
    public ArgParser On(string shortName, string longName, Action<string> handler, string? valueName = null)
    {
        _defs.Add(new ArgDef(shortName, longName, handler, null, valueName ?? longName.TrimStart('-'), IsFlag: false));
        return this;
    }

    /// <summary>Register an option with only a long name.</summary>
    public ArgParser On(string longName, Action<string> handler, string? valueName = null)
    {
        _defs.Add(new ArgDef(null, longName, handler, null, valueName ?? longName.TrimStart('-'), IsFlag: false));
        return this;
    }

    /// <summary>Register a flag (no value, e.g., --help).</summary>
    public ArgParser Flag(string shortName, string longName, Action handler)
    {
        _defs.Add(new ArgDef(shortName, longName, null, handler, null, IsFlag: true));
        return this;
    }

    /// <summary>Register a flag with only a long name.</summary>
    public ArgParser Flag(string longName, Action handler)
    {
        _defs.Add(new ArgDef(null, longName, null, handler, null, IsFlag: true));
        return this;
    }

    /// <summary>Parse all arguments. Throws ArgumentException for unknown args or missing values.</summary>
    public void Parse()
    {
        for (var i = 0; i < _args.Length; i++)
        {
            var arg = _args[i];
            var matched = false;

            foreach (var def in _defs)
            {
                if (arg != def.ShortName && arg != def.LongName)
                    continue;

                matched = true;

                if (def.IsFlag)
                {
                    def.FlagHandler!();
                }
                else
                {
                    if (i + 1 >= _args.Length)
                        throw new ArgumentException($"Missing value for {def.ValueName}.");
                    def.ValueHandler!(_args[++i]);
                }
                break;
            }

            if (!matched)
                throw new ArgumentException($"Unknown argument: {arg}");
        }
    }

    private sealed record ArgDef(
        string? ShortName,
        string LongName,
        Action<string>? ValueHandler,
        Action? FlagHandler,
        string? ValueName,
        bool IsFlag);
}
