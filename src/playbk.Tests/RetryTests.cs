using playbk.Execution;
using Xunit;

namespace playbk.Tests;

public class RetryTests
{
    // delaySeconds = 0 throughout so tests don't actually wait.

    [Fact]
    public async Task Succeeds_FirstAttempt_RunsOnce()
    {
        var calls = 0;
        var ok = await ScriptRunner.RunWithRetryAsync(retries: 3, delaySeconds: 0, "step",
            () => { calls++; return Task.FromResult(true); }, CancellationToken.None);

        Assert.True(ok);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ZeroRetries_FailsFast_RunsOnce()
    {
        var calls = 0;
        var ok = await ScriptRunner.RunWithRetryAsync(retries: 0, delaySeconds: 0, "step",
            () => { calls++; return Task.FromResult(false); }, CancellationToken.None);

        Assert.False(ok);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task RetriesExhausted_RunsOnePlusRetriesTimes()
    {
        var calls = 0;
        var ok = await ScriptRunner.RunWithRetryAsync(retries: 2, delaySeconds: 0, "step",
            () => { calls++; return Task.FromResult(false); }, CancellationToken.None);

        Assert.False(ok);
        Assert.Equal(3, calls); // 1 + 2 retries
    }

    [Fact]
    public async Task SucceedsOnLaterAttempt_StopsRetrying()
    {
        var calls = 0;
        var ok = await ScriptRunner.RunWithRetryAsync(retries: 5, delaySeconds: 0, "step",
            () => { calls++; return Task.FromResult(calls == 3); }, CancellationToken.None);

        Assert.True(ok);
        Assert.Equal(3, calls); // succeeded on the 3rd, no further attempts
    }

    [Fact]
    public async Task NegativeRetries_TreatedAsZero()
    {
        var calls = 0;
        var ok = await ScriptRunner.RunWithRetryAsync(retries: -5, delaySeconds: 0, "step",
            () => { calls++; return Task.FromResult(false); }, CancellationToken.None);

        Assert.False(ok);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Cancellation_BeforeAttempt_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ScriptRunner.RunWithRetryAsync(retries: 3, delaySeconds: 0, "step",
                () => Task.FromResult(false), cts.Token));
    }
}
