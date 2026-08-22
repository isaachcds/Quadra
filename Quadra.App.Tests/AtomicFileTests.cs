using Quadra.App.Infrastructure;

namespace Quadra.App.Tests;

public sealed class AtomicFileTests
{
    [Fact]
    public async Task WriteAsync_RemovesPartialAfterIOException()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"quadra-tests-{Guid.NewGuid():N}");
        var finalPath = Path.Combine(directory, "sample.bin");

        try
        {
            await Assert.ThrowsAsync<IOException>(() => AtomicFile.WriteAsync(
                finalPath,
                async stream =>
                {
                    await stream.WriteAsync(new byte[] { 1, 2, 3 });
                    throw new IOException("simulated");
                }));

            Assert.False(File.Exists(finalPath));
            Assert.False(File.Exists(finalPath + ".partial"));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
