namespace Quadra.App.Infrastructure;

public sealed class AsyncInitializationGate
{
    private readonly object _sync = new();
    private Task? _initializationTask;

    public async Task EnsureInitializedAsync(Func<Task> initializeAsync)
    {
        ArgumentNullException.ThrowIfNull(initializeAsync);

        Task task;

        lock (_sync)
        {
            if (_initializationTask is null ||
                _initializationTask.IsCanceled ||
                _initializationTask.IsFaulted)
            {
                _initializationTask = initializeAsync();
            }

            task = _initializationTask;
        }

        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            lock (_sync)
            {
                if (ReferenceEquals(_initializationTask, task))
                    _initializationTask = null;
            }

            throw;
        }
    }
}
