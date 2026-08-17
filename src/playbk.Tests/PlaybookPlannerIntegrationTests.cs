using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using playbk.Ai;
using Xunit;

namespace playbk.Tests;

/// <summary>
/// Live end-to-end test of the AI generator: hand the resolved LLM registry the
/// user's own example goal and assert it produces a playbook that parses and
/// validates. Uses whatever backend the host has (embedded/Ollama/cloud).
///
/// The "IntegrationTests" suffix is excluded by the CI filter. Run locally:
///   dotnet test src/playbk.Tests --filter "FullyQualifiedName~IntegrationTests"
/// </summary>
public class PlaybookPlannerIntegrationTests
{
    [Fact]
    public async Task Generate_NotepadGoal_ProducesRunnablePlaybook()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10)); // first run may download a model
        var (raw, backend, errors) = await PlaybookPlanner.GenerateAsync(
            "write 'hello world' in notepad and save it as hello_world.txt", cts.Token);

        Assert.True(!string.IsNullOrWhiteSpace(raw),
            $"planner returned nothing (backend={backend}): {string.Join(" · ", errors)}");

        var (script, validation) = PlaybookPlanner.ParseAndValidate(raw);
        Assert.True(script is not null,
            $"generated playbook did not validate: {string.Join(" · ", validation)}\n--- raw ---\n{raw}");

        // A sane plan for this goal launches notepad somewhere.
        Assert.Contains(script!.Steps, s =>
            (s.Args ?? "").Contains("notepad", StringComparison.OrdinalIgnoreCase) ||
            (s.Window ?? "").Contains("notepad", StringComparison.OrdinalIgnoreCase));
    }
}
