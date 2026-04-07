using cnvrtr;
using Xunit;

namespace cnvrtr.Tests;

public class EncodingTests
{
    [Fact]
    public void PlaintextToBase64() => Assert.Equal("aGVsbG8=", Program.Convert("hello", null, "base64"));

    [Fact]
    public void Base64ToPlaintext() => Assert.Equal("hello", Program.Convert("aGVsbG8=", "base64", "plaintext"));

    [Fact]
    public void PlaintextToHex_Number() => Assert.Equal("ff", Program.Convert("255", null, "hex"));

    [Fact]
    public void HexToDecimal() => Assert.Equal("255", Program.Convert("ff", "hex", "decimal"));

    [Fact]
    public void PlaintextToUrl() => Assert.Equal("hello+world", Program.Convert("hello world", null, "url"));

    [Fact]
    public void UrlToPlaintext() => Assert.Equal("hello world", Program.Convert("hello+world", "url", "plaintext"));

    [Fact]
    public void PlaintextToHtml() => Assert.Equal("&lt;div&gt;", Program.Convert("<div>", null, "html"));

    [Fact]
    public void HtmlToPlaintext() => Assert.Equal("<div>", Program.Convert("&lt;div&gt;", "html", "plaintext"));

    [Fact]
    public void PlaintextToBinary() => Assert.Equal("01001000 01101001", Program.Convert("Hi", null, "binary"));

    [Fact]
    public void Base64RoundTrip()
    {
        var encoded = Program.Convert("IdleOps toolkit!", null, "base64");
        var decoded = Program.Convert(encoded, "base64", "plaintext");
        Assert.Equal("IdleOps toolkit!", decoded);
    }
}

public class HashingTests
{
    [Fact]
    public void Md5Hash() => Assert.Equal("5d41402abc4b2a76b9719d911017c592", Program.Convert("hello", null, "md5"));

    [Fact]
    public void Sha256Hash() => Assert.Equal("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", Program.Convert("hello", null, "sha256"));

    [Fact]
    public void Sha1Hash() => Assert.Equal("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d", Program.Convert("hello", null, "sha1"));

    [Fact]
    public void Sha512ProducesLongHash()
    {
        var hash = Program.Convert("hello", null, "sha512");
        Assert.Equal(128, hash.Length); // 512 bits = 128 hex chars
    }
}

public class StringTransformTests
{
    [Fact]
    public void ToUpper() => Assert.Equal("HELLO WORLD", Program.Convert("Hello World", null, "upper"));

    [Fact]
    public void ToLower() => Assert.Equal("hello world", Program.Convert("Hello World", null, "lower"));

    [Fact]
    public void ToTitle() => Assert.Equal("Hello World", Program.Convert("hello world", null, "title"));

    [Fact]
    public void Reverse() => Assert.Equal("dlrow olleh", Program.Convert("hello world", null, "reverse"));

    [Fact]
    public void Length() => Assert.Equal("11", Program.Convert("hello world", null, "length"));

    [Fact]
    public void WordCount() => Assert.Equal("2", Program.Convert("hello world", null, "wordcount"));

    [Fact]
    public void Slug() => Assert.Equal("hello-world", Program.Convert("Hello World", null, "slug"));

    [Fact]
    public void SnakeCase() => Assert.Equal("hello_world", Program.Convert("Hello World", null, "snake"));

    [Fact]
    public void CamelCase() => Assert.Equal("helloWorld", Program.Convert("hello world", null, "camel"));

    [Fact]
    public void PascalCase() => Assert.Equal("HelloWorld", Program.Convert("hello world", null, "pascal"));

    [Fact]
    public void Trim() => Assert.Equal("hello", Program.Convert("  hello  ", null, "trim"));
}

public class NumberBaseTests
{
    [Fact]
    public void DecimalToHex() => Assert.Equal("ff", Program.Convert("255", "decimal", "hex"));

    [Fact]
    public void HexToDecimal2() => Assert.Equal("4096", Program.Convert("1000", "hex", "decimal"));

    [Fact]
    public void DecimalToOctal() => Assert.Equal("377", Program.Convert("255", "decimal", "octal"));

    [Fact]
    public void OctalToDecimal() => Assert.Equal("255", Program.Convert("377", "octal", "decimal"));

    [Fact]
    public void DecimalToBinary() => Assert.Equal("11111111", Program.Convert("255", "dec", "bin"));

    [Fact]
    public void BinaryToDecimal() => Assert.Equal("255", Program.Convert("11111111", "binary", "decimal"));

    [Fact]
    public void HexToOctal() => Assert.Equal("377", Program.Convert("ff", "hex", "octal"));

    [Fact]
    public void BinaryToHex() => Assert.Equal("ff", Program.Convert("11111111", "binary", "hex"));
}

public class DateTimeTests
{
    [Fact]
    public void DateToUnix() => Assert.Equal("1775433600", Program.Convert("2026-04-06T00:00:00Z", null, "unix"));

    [Fact]
    public void UnixToIso8601()
    {
        var result = Program.Convert("1775433600", "unix", "iso8601");
        Assert.StartsWith("2026-04-06", result);
    }

    [Fact]
    public void DateToYear() => Assert.Equal("2026", Program.Convert("2026-04-06T00:00:00Z", null, "year"));

    [Fact]
    public void DateToMonth() => Assert.Equal("4", Program.Convert("2026-04-06T00:00:00Z", null, "month"));

    [Fact]
    public void DateToDay() => Assert.Equal("6", Program.Convert("2026-04-06T00:00:00Z", null, "day"));

    [Fact]
    public void DateToDayOfWeek() => Assert.Equal("Monday", Program.Convert("2026-04-06T00:00:00Z", null, "dayofweek"));

    [Fact]
    public void DateToQuarter() => Assert.Equal("2", Program.Convert("2026-04-06T00:00:00Z", null, "quarter"));

    [Fact]
    public void UnixRoundTrip()
    {
        var unix = Program.Convert("2026-04-06T00:00:00Z", null, "unix");
        var iso = Program.Convert(unix, "unix", "iso8601");
        Assert.StartsWith("2026-04-06", iso);
    }
}

public class TemperatureTests
{
    [Fact]
    public void CelsiusToFahrenheit() => Assert.Equal("212", Program.Convert("100", "celsius", "fahrenheit"));

    [Fact]
    public void FahrenheitToCelsius() => Assert.Equal("100", Program.Convert("212", "fahrenheit", "celsius"));

    [Fact]
    public void CelsiusToKelvin() => Assert.Equal("373.15", Program.Convert("100", "celsius", "kelvin"));

    [Fact]
    public void KelvinToCelsius() => Assert.Equal("0", Program.Convert("273.15", "kelvin", "celsius"));

    [Fact]
    public void FreezingPointF() => Assert.Equal("32", Program.Convert("0", "celsius", "fahrenheit"));

    [Fact]
    public void AbsoluteZero() => Assert.Equal("-273.15", Program.Convert("0", "kelvin", "celsius"));

    [Fact]
    public void BodyTemp() => Assert.Equal("98.6", Program.Convert("37", "celsius", "fahrenheit"));

    [Theory]
    [InlineData("c", "f")]
    [InlineData("celsius", "fahrenheit")]
    public void ShortAndLongNamesWork(string from, string to)
    {
        Assert.Equal("212", Program.Convert("100", from, to));
    }
}

public class LengthTests
{
    [Fact]
    public void MetresToFeet()
    {
        var result = double.Parse(Program.Convert("1", "metres", "feet"));
        Assert.InRange(result, 3.2808, 3.2809);
    }

    [Fact]
    public void FeetToMetres()
    {
        var result = double.Parse(Program.Convert("6", "feet", "metres"));
        Assert.InRange(result, 1.828, 1.829);
    }

    [Fact]
    public void MilesToKm()
    {
        var result = double.Parse(Program.Convert("1", "miles", "km"));
        Assert.InRange(result, 1.609, 1.610);
    }

    [Fact]
    public void KmToMiles()
    {
        var result = double.Parse(Program.Convert("5", "km", "miles"));
        Assert.InRange(result, 3.106, 3.108);
    }

    [Fact]
    public void InchesToCm()
    {
        var result = double.Parse(Program.Convert("1", "inches", "cm"));
        Assert.InRange(result, 2.539, 2.541);
    }

    [Fact]
    public void YardsToMetres()
    {
        var result = double.Parse(Program.Convert("1", "yards", "metres"));
        Assert.InRange(result, 0.914, 0.915);
    }

    [Theory]
    [InlineData("mm", "cm", "10", 1.0)]
    [InlineData("cm", "mm", "1", 10.0)]
    [InlineData("m", "cm", "1", 100.0)]
    [InlineData("km", "m", "1", 1000.0)]
    public void MetricConversions(string from, string to, string value, double expected)
    {
        var result = double.Parse(Program.Convert(value, from, to));
        Assert.Equal(expected, result, 2);
    }
}

public class MassTests
{
    [Fact]
    public void KgToLbs()
    {
        var result = double.Parse(Program.Convert("1", "kg", "lbs"));
        Assert.InRange(result, 2.204, 2.206);
    }

    [Fact]
    public void LbsToKg()
    {
        var result = double.Parse(Program.Convert("10", "lbs", "kg"));
        Assert.InRange(result, 4.535, 4.537);
    }

    [Fact]
    public void OzToGrams()
    {
        var result = double.Parse(Program.Convert("1", "oz", "grams"));
        Assert.InRange(result, 28.34, 28.36);
    }

    [Fact]
    public void StoneToKg()
    {
        var result = double.Parse(Program.Convert("1", "stone", "kg"));
        Assert.InRange(result, 6.35, 6.36);
    }

    [Fact]
    public void GramsToKg() => Assert.Equal("1", Program.Convert("1000", "grams", "kg"));

    [Fact]
    public void TonneToKg() => Assert.Equal("1000", Program.Convert("1", "tonnes", "kg"));
}

public class DataStorageTests
{
    [Fact]
    public void BytesToKb() => Assert.Equal("1", Program.Convert("1024", "bytes", "kb"));

    [Fact]
    public void KbToMb() => Assert.Equal("1", Program.Convert("1024", "kb", "mb"));

    [Fact]
    public void MbToGb() => Assert.Equal("1", Program.Convert("1024", "mb", "gb"));

    [Fact]
    public void GbToTb() => Assert.Equal("1", Program.Convert("1024", "gb", "tb"));

    [Fact]
    public void BytesToBits() => Assert.Equal("8", Program.Convert("1", "bytes", "bits"));

    [Fact]
    public void BitsToBytes() => Assert.Equal("1", Program.Convert("8", "bits", "bytes"));
}

public class SpeedTests
{
    [Fact]
    public void KphToMph()
    {
        var result = double.Parse(Program.Convert("100", "kph", "mph"));
        Assert.InRange(result, 62.13, 62.15);
    }

    [Fact]
    public void MphToKph()
    {
        var result = double.Parse(Program.Convert("60", "mph", "kph"));
        Assert.InRange(result, 96.5, 96.6);
    }

    [Fact]
    public void KnotsToKph()
    {
        var result = double.Parse(Program.Convert("1", "knots", "kph"));
        Assert.InRange(result, 1.85, 1.86);
    }
}

public class VolumeTests
{
    [Fact]
    public void LitresToGallons()
    {
        var result = double.Parse(Program.Convert("3.78541", "litres", "gallons"));
        Assert.InRange(result, 0.99, 1.01);
    }

    [Fact]
    public void MlToLitres() => Assert.Equal("1", Program.Convert("1000", "ml", "litres"));

    [Fact]
    public void CupsToMl()
    {
        var result = double.Parse(Program.Convert("1", "cups", "ml"));
        Assert.InRange(result, 236, 237);
    }

    [Fact]
    public void TbspToTsp()
    {
        var result = double.Parse(Program.Convert("1", "tbsp", "tsp"));
        Assert.InRange(result, 2.99, 3.01);
    }
}

public class PressureTests
{
    [Fact]
    public void AtmToPascals()
    {
        var result = double.Parse(Program.Convert("1", "atm", "pa"));
        Assert.Equal(101325, result, 0);
    }

    [Fact]
    public void PsiToBar()
    {
        var result = double.Parse(Program.Convert("14.5038", "psi", "bar"));
        Assert.InRange(result, 0.99, 1.01);
    }
}

public class EnergyTests
{
    [Fact]
    public void CaloriesToJoules()
    {
        var result = double.Parse(Program.Convert("1", "calories", "joules"));
        Assert.Equal(4.184, result, 3);
    }

    [Fact]
    public void KwhToJoules()
    {
        var result = double.Parse(Program.Convert("1", "kwh", "joules"));
        Assert.Equal(3600000, result, 0);
    }
}

public class AngleTests
{
    [Fact]
    public void DegreesToRadians()
    {
        var result = double.Parse(Program.Convert("180", "degrees", "radians"));
        Assert.InRange(result, 3.1415, 3.1416);
    }

    [Fact]
    public void RadiansToDegrees()
    {
        var result = double.Parse(Program.Convert("3.14159", "radians", "degrees"));
        Assert.InRange(result, 179.99, 180.01);
    }

    [Fact]
    public void TurnsToDegrees() => Assert.Equal("360", Program.Convert("1", "turns", "degrees"));
}

public class EdgeCaseTests
{
    [Fact]
    public void ZeroCelsiusIs32F() => Assert.Equal("32", Program.Convert("0", "celsius", "fahrenheit"));

    [Fact]
    public void LargeNumber()
    {
        var result = Program.Convert("1000000", "bytes", "mb");
        Assert.NotNull(result);
    }

    [Fact]
    public void UnsupportedConversionThrows()
    {
        Assert.Throws<ArgumentException>(() => Program.Convert("hello", "nonsense", "garbage"));
    }
}
