namespace Quadra.App.Infrastructure;

public static class AtomicFile
{
    public static async Task WriteAsync(
        string finalPath,
        Func<Stream, Task> writeAsync,
        Func<string, Task>? validateAsync = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(finalPath);
        ArgumentNullException.ThrowIfNull(writeAsync);

        var partialPath = finalPath + ".partial";
        DeleteIfExists(partialPath);

        try
        {
            var directory = Path.GetDirectoryName(finalPath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using (var stream = new FileStream(
                             partialPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             81920,
                             useAsync: true))
            {
                await writeAsync(stream);
                await stream.FlushAsync(cancellationToken);

                if (stream.Length <= 0)
                    throw new InvalidDataException("O arquivo processado ficou vazio.");
            }

            if (validateAsync is not null)
                await validateAsync(partialPath);

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(partialPath, finalPath, overwrite: true);
        }
        catch
        {
            DeleteIfExists(partialPath);
            throw;
        }
    }

    public static void Write(
        string finalPath,
        Action<Stream> write,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(finalPath);
        ArgumentNullException.ThrowIfNull(write);

        var partialPath = finalPath + ".partial";
        DeleteIfExists(partialPath);

        try
        {
            var directory = Path.GetDirectoryName(finalPath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            using (var stream = new FileStream(
                       partialPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                write(stream);
                stream.Flush(flushToDisk: true);

                if (stream.Length <= 0)
                    throw new InvalidDataException("O arquivo processado ficou vazio.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(partialPath, finalPath, overwrite: true);
        }
        catch
        {
            DeleteIfExists(partialPath);
            throw;
        }
    }

    public static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
