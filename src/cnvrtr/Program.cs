using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Diagnostics;

namespace cnvrtr;

internal static class Program
{
    private static int Main(string[] args)
    {
        string? value = null;
        string? from = null;
        string? to = null;
        bool showHelp = false;
        bool listFormats = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--value":
                    if (i + 1 >= args.Length) { Console.Error.WriteLine("Missing value."); return 1; }
                    value = args[++i];
                    break;
                case "--from":
                    if (i + 1 >= args.Length) { Console.Error.WriteLine("Missing from format."); return 1; }
                    from = args[++i].ToLowerInvariant();
                    break;
                case "--to":
                    if (i + 1 >= args.Length) { Console.Error.WriteLine("Missing to format."); return 1; }
                    to = args[++i].ToLowerInvariant();
                    break;
                case "--list":
                    listFormats = true;
                    break;
                case "-h":
                case "--help":
                    showHelp = true;
                    break;
                case "--version":
                    Console.WriteLine($"cnvrtr {VersionLine()}");
                    return 0;
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[i]}");
                    return 1;
            }
        }

        if (showHelp)
        {
            PrintHelp();
            return 0;
        }

        if (listFormats)
        {
            PrintFormats();
            return 0;
        }

        // Read from stdin if no value provided
        if (value is null)
        {
            if (!Console.IsInputRedirected)
            {
                Console.Error.WriteLine("No --value specified and no stdin piped. Use --help for usage.");
                return 1;
            }
            value = Console.In.ReadToEnd().TrimEnd('\r', '\n');
        }

        if (to is null)
        {
            Console.Error.WriteLine("--to is required.");
            return 1;
        }

        try
        {
            var result = Convert(value, from, to);
            Console.WriteLine(result);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Conversion failed: {ex.Message}");
            return 1;
        }
    }

    internal static string Convert(string value, string? from, string to)
    {
        // --- Number base conversions (check first — "decimal"/"binary" overlap with encoding names) ---
        var baseResult = TryBaseConversion(value, from, to);
        if (baseResult is not null) return baseResult;

        // --- Encoding / Hashing ---
        var encodingResult = TryEncoding(value, from, to);
        if (encodingResult is not null) return encodingResult;

        // --- Date / Time ---
        var dateResult = TryDateConversion(value, from, to);
        if (dateResult is not null) return dateResult;

        // --- Unit conversions (table-driven) ---
        var unitResult = TryUnitConversion(value, from, to);
        if (unitResult is not null) return unitResult;

        // --- File conversions (ffmpeg) ---
        var fileResult = TryFileConversion(value, from, to);
        if (fileResult is not null) return fileResult;

        throw new ArgumentException($"Unsupported conversion: {from ?? "auto"} → {to}. Use --list to see formats.");
    }

    // ========== ENCODING / HASHING ==========

    private static string? TryEncoding(string value, string? from, string to)
    {
        // Decode from source format to bytes
        byte[]? bytes = from switch
        {
            "base64" => System.Convert.FromBase64String(value),
            "hex" when IsHexString(value) => System.Convert.FromHexString(value),
            "url" or "urlencode" or "percent" => Encoding.UTF8.GetBytes(HttpUtility.UrlDecode(value)),
            "html" or "htmlencode" => Encoding.UTF8.GetBytes(HttpUtility.HtmlDecode(value)),
            "binary" or "bin" => BinaryToBytes(value),
            "utf8" or "text" or "plaintext" or null => null, // will use raw string
            _ => null
        };

        var sourceBytes = bytes ?? Encoding.UTF8.GetBytes(value);

        return to switch
        {
            // Encoding
            "base64" when from is null or "text" or "plaintext" or "utf8" => System.Convert.ToBase64String(sourceBytes),
            "hex" when from is null or "text" or "plaintext" or "decimal" =>
                long.TryParse(value, out var n) ? n.ToString("x") : System.Convert.ToHexString(sourceBytes).ToLowerInvariant(),
            "url" or "urlencode" or "percent" => HttpUtility.UrlEncode(from is "url" ? HttpUtility.UrlDecode(value) : value),
            "html" or "htmlencode" => HttpUtility.HtmlEncode(from is "html" ? HttpUtility.HtmlDecode(value) : value),
            "binary" or "bin" => string.Join(" ", sourceBytes.Select(b => System.Convert.ToString(b, 2).PadLeft(8, '0'))),
            "utf8" or "text" or "plaintext" when from is not null => Encoding.UTF8.GetString(sourceBytes),
            "ascii" => Encoding.ASCII.GetString(sourceBytes),
            "unicode" or "utf16" => Encoding.Unicode.GetString(sourceBytes),

            // Hashing
            "md5" => HashToHex(MD5.HashData(sourceBytes)),
            "sha1" => HashToHex(SHA1.HashData(sourceBytes)),
            "sha256" => HashToHex(SHA256.HashData(sourceBytes)),
            "sha384" => HashToHex(SHA384.HashData(sourceBytes)),
            "sha512" => HashToHex(SHA512.HashData(sourceBytes)),

            // String transforms
            "upper" or "uppercase" => value.ToUpperInvariant(),
            "lower" or "lowercase" => value.ToLowerInvariant(),
            "title" or "titlecase" => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant()),
            "reverse" => new string(value.Reverse().ToArray()),
            "length" or "len" => value.Length.ToString(),
            "wordcount" or "words" => value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length.ToString(),
            "linecount" or "lines" => value.Split('\n').Length.ToString(),
            "trim" => value.Trim(),
            "slug" or "kebab" => Slugify(value),
            "snake" or "snake_case" => Slugify(value).Replace('-', '_'),
            "camel" or "camelcase" => ToCamelCase(value),
            "pascal" or "pascalcase" => ToPascalCase(value),

            _ => null
        };
    }

    // ========== DATE / TIME ==========

    private static string? TryDateConversion(string value, string? from, string to)
    {
        DateTimeOffset? dto = from switch
        {
            "unix" => DateTimeOffset.FromUnixTimeSeconds(long.Parse(value)),
            "unixms" => DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(value)),
            "iso8601" or "iso" or "datetime" or null when DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) => parsed,
            _ => null
        };

        if (dto is null) return null;

        return to switch
        {
            "unix" => dto.Value.ToUnixTimeSeconds().ToString(),
            "unixms" => dto.Value.ToUnixTimeMilliseconds().ToString(),
            "iso8601" or "iso" => dto.Value.UtcDateTime.ToString("o"),
            "date" => dto.Value.UtcDateTime.ToString("yyyy-MM-dd"),
            "time" => dto.Value.UtcDateTime.ToString("HH:mm:ss"),
            "datetime" => dto.Value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            "rfc2822" or "rfc" => dto.Value.ToString("ddd, dd MMM yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture),
            "year" => dto.Value.Year.ToString(),
            "month" => dto.Value.Month.ToString(),
            "day" => dto.Value.Day.ToString(),
            "dayofweek" or "dow" => dto.Value.DayOfWeek.ToString(),
            "dayofyear" or "doy" => dto.Value.DayOfYear.ToString(),
            "quarter" => ((dto.Value.Month - 1) / 3 + 1).ToString(),
            "relative" or "ago" => ToRelativeTime(dto.Value),
            _ => null
        };
    }

    // ========== UNIT CONVERSIONS (table-driven) ==========

    // All units stored as ratio to a base unit per category
    private static readonly Dictionary<string, Dictionary<string, double>> UnitTables = new()
    {
        // Time (base: seconds)
        ["time"] = new()
        {
            ["nanoseconds"] = 1e-9, ["ns"] = 1e-9,
            ["microseconds"] = 1e-6, ["us"] = 1e-6, ["μs"] = 1e-6,
            ["milliseconds"] = 0.001, ["ms"] = 0.001,
            ["seconds"] = 1, ["s"] = 1, ["sec"] = 1,
            ["minutes"] = 60, ["min"] = 60, ["m"] = 60,
            ["hours"] = 3600, ["h"] = 3600, ["hr"] = 3600,
            ["days"] = 86400, ["d"] = 86400,
            ["weeks"] = 604800, ["w"] = 604800, ["wk"] = 604800,
            ["months"] = 2629746, ["mo"] = 2629746,  // average
            ["years"] = 31556952, ["y"] = 31556952, ["yr"] = 31556952,  // average
            ["decades"] = 315569520,
            ["centuries"] = 3155695200,
        },

        // Length (base: metres)
        ["length"] = new()
        {
            ["nanometres"] = 1e-9, ["nm"] = 1e-9, ["nanometers"] = 1e-9,
            ["micrometres"] = 1e-6, ["um"] = 1e-6, ["μm"] = 1e-6, ["micrometers"] = 1e-6, ["microns"] = 1e-6,
            ["millimetres"] = 0.001, ["mm"] = 0.001, ["millimeters"] = 0.001,
            ["centimetres"] = 0.01, ["cm"] = 0.01, ["centimeters"] = 0.01,
            ["metres"] = 1, ["m"] = 1, ["meters"] = 1,
            ["kilometres"] = 1000, ["km"] = 1000, ["kilometers"] = 1000,
            ["inches"] = 0.0254, ["in"] = 0.0254, ["inch"] = 0.0254,
            ["feet"] = 0.3048, ["ft"] = 0.3048, ["foot"] = 0.3048,
            ["yards"] = 0.9144, ["yd"] = 0.9144,
            ["miles"] = 1609.344, ["mi"] = 1609.344,
            ["nautical-miles"] = 1852, ["nmi"] = 1852,
            ["fathoms"] = 1.8288,
            ["furlongs"] = 201.168,
            ["lightyears"] = 9.461e+15, ["ly"] = 9.461e+15,
            ["parsecs"] = 3.086e+16, ["pc"] = 3.086e+16,
            ["au"] = 1.496e+11, // astronomical unit
            ["angstroms"] = 1e-10,
            ["mils"] = 2.54e-5, ["thou"] = 2.54e-5,
        },

        // Mass / Weight (base: kilograms)
        ["mass"] = new()
        {
            ["micrograms"] = 1e-9, ["ug"] = 1e-9, ["μg"] = 1e-9, ["mcg"] = 1e-9,
            ["milligrams"] = 1e-6, ["mg"] = 1e-6,
            ["grams"] = 0.001, ["g"] = 0.001,
            ["kilograms"] = 1, ["kg"] = 1,
            ["tonnes"] = 1000, ["t"] = 1000, ["metric-tons"] = 1000,
            ["ounces"] = 0.0283495, ["oz"] = 0.0283495,
            ["pounds"] = 0.453592, ["lb"] = 0.453592, ["lbs"] = 0.453592,
            ["stone"] = 6.35029, ["st"] = 6.35029,
            ["short-tons"] = 907.185, ["us-tons"] = 907.185,
            ["long-tons"] = 1016.05, ["imperial-tons"] = 1016.05,
            ["carats"] = 0.0002, ["ct"] = 0.0002,
            ["grains"] = 6.47989e-5, ["gr"] = 6.47989e-5,
        },

        // Temperature (special handling — not simple ratio)
        // Handled separately in TryTemperature()

        // Digital storage (base: bytes)
        ["data"] = new()
        {
            ["bits"] = 0.125, ["b"] = 0.125, ["bit"] = 0.125,
            ["nibbles"] = 0.5,
            ["bytes"] = 1, ["byte"] = 1,
            ["kb"] = 1024, ["kib"] = 1024, ["kilobytes"] = 1024,
            ["mb"] = 1048576, ["mib"] = 1048576, ["megabytes"] = 1048576,
            ["gb"] = 1073741824, ["gib"] = 1073741824, ["gigabytes"] = 1073741824,
            ["tb"] = 1099511627776.0, ["tib"] = 1099511627776.0, ["terabytes"] = 1099511627776.0,
            ["pb"] = 1125899906842624.0, ["pib"] = 1125899906842624.0, ["petabytes"] = 1125899906842624.0,
            // SI (powers of 1000)
            ["kbsi"] = 1000, ["mbsi"] = 1e6, ["gbsi"] = 1e9, ["tbsi"] = 1e12,
        },

        // Speed (base: m/s)
        ["speed"] = new()
        {
            ["m/s"] = 1, ["mps"] = 1, ["metres-per-second"] = 1, ["meters-per-second"] = 1,
            ["km/h"] = 0.277778, ["kph"] = 0.277778, ["kmh"] = 0.277778,
            ["mph"] = 0.44704, ["miles-per-hour"] = 0.44704,
            ["knots"] = 0.514444, ["kn"] = 0.514444, ["kt"] = 0.514444,
            ["ft/s"] = 0.3048, ["fps"] = 0.3048,
            ["mach"] = 343, // at sea level, 20°C
            ["c"] = 299792458, ["lightspeed"] = 299792458,
        },

        // Area (base: square metres)
        ["area"] = new()
        {
            ["mm2"] = 1e-6, ["sq-mm"] = 1e-6,
            ["cm2"] = 1e-4, ["sq-cm"] = 1e-4,
            ["m2"] = 1, ["sq-m"] = 1, ["square-metres"] = 1, ["square-meters"] = 1,
            ["km2"] = 1e6, ["sq-km"] = 1e6,
            ["hectares"] = 10000, ["ha"] = 10000,
            ["acres"] = 4046.86,
            ["sq-in"] = 0.00064516, ["in2"] = 0.00064516,
            ["sq-ft"] = 0.092903, ["ft2"] = 0.092903,
            ["sq-yd"] = 0.836127, ["yd2"] = 0.836127,
            ["sq-mi"] = 2589988.11, ["mi2"] = 2589988.11,
        },

        // Volume (base: litres)
        ["volume"] = new()
        {
            ["millilitres"] = 0.001, ["ml"] = 0.001, ["milliliters"] = 0.001,
            ["centilitres"] = 0.01, ["cl"] = 0.01,
            ["litres"] = 1, ["l"] = 1, ["liters"] = 1,
            ["kilolitres"] = 1000, ["kl"] = 1000,
            ["cubic-cm"] = 0.001, ["cc"] = 0.001, ["cm3"] = 0.001,
            ["cubic-m"] = 1000, ["m3"] = 1000,
            ["cubic-in"] = 0.0163871, ["in3"] = 0.0163871,
            ["cubic-ft"] = 28.3168, ["ft3"] = 28.3168,
            ["us-gallons"] = 3.78541, ["gal"] = 3.78541, ["gallons"] = 3.78541,
            ["us-quarts"] = 0.946353, ["qt"] = 0.946353,
            ["us-pints"] = 0.473176, ["pt"] = 0.473176,
            ["us-cups"] = 0.236588, ["cups"] = 0.236588,
            ["us-fl-oz"] = 0.0295735, ["fl-oz"] = 0.0295735,
            ["us-tablespoons"] = 0.0147868, ["tbsp"] = 0.0147868,
            ["us-teaspoons"] = 0.00492892, ["tsp"] = 0.00492892,
            ["imperial-gallons"] = 4.54609, ["imp-gal"] = 4.54609,
            ["imperial-pints"] = 0.568261, ["imp-pt"] = 0.568261,
            ["imperial-fl-oz"] = 0.0284131, ["imp-fl-oz"] = 0.0284131,
            ["barrels"] = 158.987, ["bbl"] = 158.987,
        },

        // Pressure (base: pascals)
        ["pressure"] = new()
        {
            ["pascals"] = 1, ["pa"] = 1,
            ["kilopascals"] = 1000, ["kpa"] = 1000,
            ["megapascals"] = 1e6, ["mpa"] = 1e6,
            ["bar"] = 100000,
            ["millibar"] = 100, ["mbar"] = 100,
            ["atmospheres"] = 101325, ["atm"] = 101325,
            ["psi"] = 6894.76, ["pounds-per-sq-in"] = 6894.76,
            ["torr"] = 133.322, ["mmhg"] = 133.322,
            ["inhg"] = 3386.39,
        },

        // Energy (base: joules)
        ["energy"] = new()
        {
            ["joules"] = 1, ["j"] = 1,
            ["kilojoules"] = 1000, ["kj"] = 1000,
            ["megajoules"] = 1e6, ["mj"] = 1e6,
            ["calories"] = 4.184, ["cal"] = 4.184,
            ["kilocalories"] = 4184, ["kcal"] = 4184,
            ["watt-hours"] = 3600, ["wh"] = 3600,
            ["kilowatt-hours"] = 3.6e6, ["kwh"] = 3.6e6,
            ["electron-volts"] = 1.602e-19, ["ev"] = 1.602e-19,
            ["btu"] = 1055.06, ["british-thermal-units"] = 1055.06,
            ["foot-pounds"] = 1.35582, ["ft-lb"] = 1.35582,
            ["therms"] = 1.055e8,
        },

        // Power (base: watts)
        ["power"] = new()
        {
            ["watts"] = 1, ["w"] = 1,
            ["kilowatts"] = 1000, ["kw"] = 1000,
            ["megawatts"] = 1e6, ["mw"] = 1e6,
            ["gigawatts"] = 1e9, ["gw"] = 1e9,
            ["horsepower"] = 745.7, ["hp"] = 745.7,
            ["metric-hp"] = 735.499, ["ps"] = 735.499,
            ["btu/h"] = 0.293071, ["btuh"] = 0.293071,
            ["ft-lb/s"] = 1.35582,
        },

        // Frequency (base: hertz)
        ["frequency"] = new()
        {
            ["hertz"] = 1, ["hz"] = 1,
            ["kilohertz"] = 1000, ["khz"] = 1000,
            ["megahertz"] = 1e6, ["mhz"] = 1e6,
            ["gigahertz"] = 1e9, ["ghz"] = 1e9,
            ["rpm"] = 1.0 / 60, ["revolutions-per-minute"] = 1.0 / 60,
        },

        // Angle (base: degrees)
        ["angle"] = new()
        {
            ["degrees"] = 1, ["deg"] = 1,
            ["radians"] = 57.2958, ["rad"] = 57.2958,
            ["gradians"] = 0.9, ["grad"] = 0.9, ["gon"] = 0.9,
            ["arcminutes"] = 1.0 / 60, ["arcmin"] = 1.0 / 60,
            ["arcseconds"] = 1.0 / 3600, ["arcsec"] = 1.0 / 3600,
            ["turns"] = 360, ["revolutions"] = 360, ["rev"] = 360,
        },

        // Fuel economy (base: km/l)
        ["fuel"] = new()
        {
            ["km/l"] = 1, ["kpl"] = 1,
            ["mpg"] = 0.425144, ["miles-per-gallon"] = 0.425144,
            ["mpg-imp"] = 0.354006, ["imperial-mpg"] = 0.354006,
            // l/100km is inverse — handled specially
        },
    };

    private static string? TryUnitConversion(string value, string? from, string to)
    {
        if (from is null) return null;
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numValue))
            return null;

        // Temperature (not ratio-based)
        var tempResult = TryTemperature(numValue, from, to);
        if (tempResult is not null) return tempResult;

        // Fuel economy special case: l/100km
        if ((from is "l/100km" && to is "mpg") || (from is "mpg" && to is "l/100km"))
        {
            if (from is "l/100km") return (235.215 / numValue).ToString("G6", CultureInfo.InvariantCulture);
            return (235.215 / numValue).ToString("G6", CultureInfo.InvariantCulture);
        }

        // Table-driven conversions
        foreach (var (_, table) in UnitTables)
        {
            if (table.TryGetValue(from, out var fromFactor) && table.TryGetValue(to, out var toFactor))
            {
                var baseValue = numValue * fromFactor;
                var result = baseValue / toFactor;
                return FormatNumber(result);
            }
        }

        return null;
    }

    private static string? TryTemperature(double value, string from, string to)
    {
        var tempNames = new HashSet<string> { "celsius", "c", "fahrenheit", "f", "kelvin", "k", "rankine", "r" };
        if (!tempNames.Contains(from) || !tempNames.Contains(to)) return null;

        // Normalize to Celsius first
        var celsius = from switch
        {
            "celsius" or "c" => value,
            "fahrenheit" or "f" => (value - 32) * 5.0 / 9.0,
            "kelvin" or "k" => value - 273.15,
            "rankine" or "r" => (value - 491.67) * 5.0 / 9.0,
            _ => throw new ArgumentException($"Unknown temperature unit: {from}")
        };

        var result = to switch
        {
            "celsius" or "c" => celsius,
            "fahrenheit" or "f" => celsius * 9.0 / 5.0 + 32,
            "kelvin" or "k" => celsius + 273.15,
            "rankine" or "r" => (celsius + 273.15) * 9.0 / 5.0,
            _ => throw new ArgumentException($"Unknown temperature unit: {to}")
        };

        return FormatNumber(result);
    }

    // ========== NUMBER BASE CONVERSIONS ==========

    private static string? TryBaseConversion(string value, string? from, string to)
    {
        long? number = from switch
        {
            "decimal" or "dec" or null when long.TryParse(value, out var d) => d,
            "hex" or "hexadecimal" when long.TryParse(value, NumberStyles.HexNumber, null, out var h) => h,
            "octal" or "oct" => System.Convert.ToInt64(value, 8),
            "binary" or "bin" when value.All(c => c is '0' or '1' or ' ') => System.Convert.ToInt64(value.Replace(" ", ""), 2),
            _ => null
        };

        if (number is null) return null;

        return to switch
        {
            "decimal" or "dec" => number.Value.ToString(),
            "hex" or "hexadecimal" => number.Value.ToString("x"),
            "octal" or "oct" => System.Convert.ToString(number.Value, 8),
            "binary" or "bin" => System.Convert.ToString(number.Value, 2),
            _ => null
        };
    }

    // ========== FILE CONVERSIONS (ffmpeg) ==========

    private static readonly HashSet<string> FfmpegFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        // Video
        "mp4", "mkv", "avi", "mov", "wmv", "flv", "webm", "m4v", "mpg", "mpeg", "3gp", "ogv", "ts",
        // Audio
        "mp3", "wav", "aac", "flac", "ogg", "wma", "m4a", "opus", "aiff", "alac",
        // Image
        "png", "jpg", "jpeg", "bmp", "gif", "tiff", "tif", "webp", "ico", "svg",
    };

    private static string? TryFileConversion(string value, string? from, string to)
    {
        // Value should be a file path
        if (!File.Exists(value)) return null;

        var sourceExt = (from ?? Path.GetExtension(value).TrimStart('.')).ToLowerInvariant();
        var targetExt = to.ToLowerInvariant();

        if (!FfmpegFormats.Contains(sourceExt) || !FfmpegFormats.Contains(targetExt))
            return null;

        var outputPath = Path.ChangeExtension(value, targetExt);

        // Use ffmpeg for video/audio, or just rename for identical formats
        if (sourceExt == targetExt)
        {
            return outputPath; // no conversion needed
        }

        Console.Error.WriteLine($"[cnvrtr] Converting '{value}' → '{outputPath}' via ffmpeg...");

        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -i \"{value}\" \"{outputPath}\"",
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null) throw new InvalidOperationException("Could not start ffmpeg.");

            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"ffmpeg failed (exit {process.ExitCode}): {stderr.Split('\n').LastOrDefault(l => l.Length > 0)}");
            }

            return outputPath;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException("ffmpeg not found on PATH. Install ffmpeg for file conversions.", ex);
        }
    }

    // ========== HELPERS ==========

    private static string FormatNumber(double value)
    {
        if (Math.Abs(value) < 0.0001 && value != 0)
            return value.ToString("E6", CultureInfo.InvariantCulture);
        if (Math.Abs(value - Math.Round(value)) < 1e-10)
            return ((long)Math.Round(value)).ToString();
        return value.ToString("G10", CultureInfo.InvariantCulture);
    }

    private static string HashToHex(byte[] hash) => System.Convert.ToHexString(hash).ToLowerInvariant();

    private static bool IsHexString(string s) => s.All(c => "0123456789abcdefABCDEF".Contains(c)) && s.Length % 2 == 0;

    private static byte[] BinaryToBytes(string binary)
    {
        var clean = binary.Replace(" ", "");
        var bytes = new byte[clean.Length / 8];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = System.Convert.ToByte(clean.Substring(i * 8, 8), 2);
        return bytes;
    }

    private static string Slugify(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c is ' ' or '_' or '-' && sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }

    private static string ToCamelCase(string s)
    {
        var words = s.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return s;
        return words[0].ToLowerInvariant() + string.Concat(words.Skip(1).Select(w =>
            char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }

    private static string ToPascalCase(string s)
    {
        var words = s.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }

    private static string ToRelativeTime(DateTimeOffset dto)
    {
        var diff = DateTimeOffset.UtcNow - dto;
        if (diff.TotalSeconds < 0) return $"in {ToRelativeSpan(-diff)}";
        return $"{ToRelativeSpan(diff)} ago";
    }

    private static string ToRelativeSpan(TimeSpan span)
    {
        if (span.TotalSeconds < 60) return $"{(int)span.TotalSeconds}s";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays}d";
        if (span.TotalDays < 365) return $"{(int)(span.TotalDays / 30)}mo";
        return $"{(int)(span.TotalDays / 365)}y";
    }

    /// <summary>
    /// Local copy of the shared BuildInfo banner. cnvrtr is the one tool that does not
    /// reference `shared` — it is a standalone utility, and taking the reference would pull
    /// System.Drawing and friends into what is currently the smallest binary in the set.
    /// Six duplicated lines is the cheaper trade.
    /// </summary>
    private static string Banner() =>
        Commit() is { Length: > 0 } c ? $"IdleOps - cnvrtr ({c})" : "IdleOps - cnvrtr";

    /// <summary>Matches the shared BuildInfo.VersionLine() shape, e.g. "1.0.0 (a27da55)".</summary>
    private static string VersionLine()
    {
        var informational = Informational();
        var plus = informational?.IndexOf('+') ?? -1;
        var version = plus >= 0 ? informational![..plus] : informational ?? "0.0.0";
        return Commit() is { Length: > 0 } c ? $"{version} ({c})" : version;
    }

    private static string? Commit()
    {
        var informational = Informational();
        var plus = informational?.IndexOf('+') ?? -1;
        return plus >= 0 && plus < informational!.Length - 1 ? informational[(plus + 1)..] : null;
    }

    private static string? Informational() => Assembly.GetEntryAssembly()
        ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    private static void PrintHelp()
    {
        Console.WriteLine(Banner());
        Console.WriteLine("""
            Usage: cnvrtr --value "<input>" [--from <format>] --to <format>
                   echo "input" | cnvrtr --to <format>

            Convert between encodings, units, date formats, number bases, and file formats.

            Examples:
              cnvrtr --value "hello" --to base64
              cnvrtr --value "ff" --from hex --to decimal
              cnvrtr --value "100" --from celsius --to fahrenheit
              cnvrtr --value "5.5" --from miles --to km
              cnvrtr --value "1024" --from bytes --to mb
              cnvrtr --value "2026-04-06" --to unix
              cnvrtr --value "hello world" --to sha256
              cnvrtr --value "Hello World" --to slug
              cnvrtr --value video.mp4 --to gif
              cnvrtr --list

            Options:
              --value     Input value (or pipe via stdin)
              --from      Source format (often auto-detected)
              --to        Target format (required)
              --list      List all supported formats
              -h, --help  Show help
            """);
    }

    private static void PrintFormats()
    {
        Console.WriteLine("""
            ENCODING:      base64, hex, url, html, binary, utf8, ascii, unicode
            HASHING:       md5, sha1, sha256, sha384, sha512
            STRING:        upper, lower, title, reverse, length, wordcount, linecount,
                           trim, slug, snake, camel, pascal
            NUMBER BASES:  decimal, hex, octal, binary
            DATE/TIME:     unix, unixms, iso8601, date, time, datetime, rfc2822,
                           year, month, day, dayofweek, dayofyear, quarter, relative
            TIME UNITS:    nanoseconds, microseconds, milliseconds, seconds, minutes,
                           hours, days, weeks, months, years, decades, centuries
            LENGTH:        nanometres, micrometres, millimetres, centimetres, metres,
                           kilometres, inches, feet, yards, miles, nautical-miles,
                           fathoms, furlongs, lightyears, parsecs, au, angstroms, mils
            MASS:          micrograms, milligrams, grams, kilograms, tonnes, ounces,
                           pounds, stone, short-tons, long-tons, carats, grains
            TEMPERATURE:   celsius, fahrenheit, kelvin, rankine
            DATA:          bits, bytes, kb, mb, gb, tb, pb (also SI variants: kbsi, mbsi...)
            SPEED:         m/s, km/h, mph, knots, ft/s, mach, c
            AREA:          mm2, cm2, m2, km2, hectares, acres, sq-ft, sq-yd, sq-mi
            VOLUME:        ml, cl, litres, gallons, quarts, pints, cups, fl-oz,
                           tablespoons, teaspoons, imperial-gallons, barrels, cc, m3
            PRESSURE:      pascals, kilopascals, bar, atmospheres, psi, torr, mmhg
            ENERGY:        joules, kilojoules, calories, kilocalories, watt-hours,
                           kilowatt-hours, electron-volts, btu, foot-pounds, therms
            POWER:         watts, kilowatts, megawatts, gigawatts, horsepower
            FREQUENCY:     hertz, kilohertz, megahertz, gigahertz, rpm
            ANGLE:         degrees, radians, gradians, arcminutes, arcseconds, turns
            FUEL:          km/l, mpg, mpg-imp, l/100km
            FILES (ffmpeg): mp4, mkv, avi, mov, webm, mp3, wav, aac, flac, ogg,
                            png, jpg, bmp, gif, tiff, webp (+ many more)
            """);
    }
}
