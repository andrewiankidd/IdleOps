using playbk.Execution;
using Xunit;

namespace playbk.Tests;

public class ScriptRunnerHelpersTests
{
    // --- Tokenize ---------------------------------------------------------

    [Fact]
    public void Tokenize_SplitsOnWhitespace()
    {
        Assert.Equal(new[] { "notepad.exe", "-flag", "value" }, ScriptRunner.Tokenize("notepad.exe -flag value"));
    }

    [Fact]
    public void Tokenize_KeepsQuotedSegmentsTogether_AndStripsQuotes()
    {
        Assert.Equal(new[] { "inpctl", "--type", "hello world" }, ScriptRunner.Tokenize("inpctl --type \"hello world\""));
    }

    [Fact]
    public void Tokenize_EmptyString_YieldsNoTokens()
    {
        Assert.Empty(ScriptRunner.Tokenize(""));
    }

    // --- SplitCommand -----------------------------------------------------

    [Fact]
    public void SplitCommand_SeparatesExecutableFromArgs()
    {
        var (file, args) = ScriptRunner.SplitCommand("notepad.exe -a -b");
        Assert.Equal("notepad.exe", file);
        Assert.Equal("-a -b", args);
    }

    [Fact]
    public void SplitCommand_NoArgs_ReturnsNullArgs()
    {
        var (file, args) = ScriptRunner.SplitCommand("notepad.exe");
        Assert.Equal("notepad.exe", file);
        Assert.Null(args);
    }

    [Fact]
    public void SplitCommand_RequotesArgsWithSpaces()
    {
        var (_, args) = ScriptRunner.SplitCommand("inpctl --type \"hello world\"");
        Assert.Equal("--type \"hello world\"", args);
    }

    // --- QuoteIfNeeded ----------------------------------------------------

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("has space", "\"has space\"")]
    public void QuoteIfNeeded_QuotesOnlyWhenWhitespacePresent(string input, string expected)
    {
        Assert.Equal(expected, ScriptRunner.QuoteIfNeeded(input));
    }

    // --- ExpandPidTokens --------------------------------------------------

    [Fact]
    public void ExpandPidTokens_ReplacesKnownId()
    {
        var pids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["rec"] = 4242 };
        Assert.Equal("inpctl --pid 4242 --ctrlc", ScriptRunner.ExpandPidTokens("inpctl --pid %rec_pid% --ctrlc", pids));
    }

    [Fact]
    public void ExpandPidTokens_IsCaseInsensitive()
    {
        var pids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Rec"] = 7 };
        Assert.Equal("7", ScriptRunner.ExpandPidTokens("%REC_pid%", pids));
    }

    [Fact]
    public void ExpandPidTokens_LeavesUnknownIdUntouched()
    {
        var pids = new Dictionary<string, int>();
        Assert.Equal("%missing_pid%", ScriptRunner.ExpandPidTokens("%missing_pid%", pids));
    }

    // --- Sanitize ---------------------------------------------------------

    [Fact]
    public void Sanitize_ReplacesSpacesAndInvalidChars()
    {
        Assert.Equal("Launch_App", ScriptRunner.Sanitize("Launch App"));
    }

    [Fact]
    public void Sanitize_EmptyBecomesStepPlaceholder()
    {
        Assert.Equal("step", ScriptRunner.Sanitize(""));
    }

    [Fact]
    public void Sanitize_WhitespaceBecomesUnderscores()
    {
        // Spaces are replaced before the blank check, so a spaces-only name is not blank.
        Assert.Equal("___", ScriptRunner.Sanitize("   "));
    }
}
