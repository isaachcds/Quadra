using Quadra.App.Services.Import;

namespace Quadra.App.Tests;

public class SupportedFileFormatsTests
{
    [Theory]
    [InlineData(".cbr")]
    [InlineData("CBZ")]
    [InlineData(".PDF")]
    [InlineData("epub")]
    public void IsSupported_AcceptsKnownFormats(string extension)
    {
        Assert.True(SupportedFileFormats.IsSupported(extension));
    }

    [Theory]
    [InlineData(".zip")]
    [InlineData(".txt")]
    [InlineData("")]
    public void IsSupported_RejectsUnknownFormats(string extension)
    {
        Assert.False(SupportedFileFormats.IsSupported(extension));
    }
}
