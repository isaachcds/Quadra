using Quadra.App.Infrastructure;

namespace Quadra.App.Tests;

public class AsyncInitializationGateTests
{
    [Fact]
    public async Task EnsureInitializedAsync_ConcurrentCalls_RunOnce()
    {
        var gate = new AsyncInitializationGate();
        var calls = 0;

        async Task InitializeAsync()
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(25);
        }

        await Task.WhenAll(
            Enumerable.Range(0, 20)
                .Select(_ => gate.EnsureInitializedAsync(InitializeAsync)));

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task EnsureInitializedAsync_AfterFailure_AllowsRetry()
    {
        var gate = new AsyncInitializationGate();
        var calls = 0;

        Task InitializeAsync()
        {
            if (Interlocked.Increment(ref calls) == 1)
                return Task.FromException(new InvalidOperationException("failure"));

            return Task.CompletedTask;
        }

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => gate.EnsureInitializedAsync(InitializeAsync));

        await gate.EnsureInitializedAsync(InitializeAsync);

        Assert.Equal(2, calls);
    }
}
