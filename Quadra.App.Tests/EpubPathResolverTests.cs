using Quadra.App.Services.Readers;

namespace Quadra.App.Tests;

public class EpubPathResolverTests
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "quadra-epub-root");

    [Theory]
    [InlineData("chapter.xhtml")]
    [InlineData("OEBPS/Text/chapter.xhtml")]
    public void ResolveInsideRoot_AcceptsValidRelativePath(string path)
    {
        var result = EpubPathResolver.ResolveInsideRoot(_root, path);

        Assert.True(EpubPathResolver.IsInsideRoot(_root, result));
    }

    [Theory]
    [InlineData("../outside.xhtml")]
    [InlineData("%2e%2e%2foutside.xhtml")]
    [InlineData("https://example.com/book.xhtml")]
    [InlineData("file:///outside.xhtml")]
    public void ResolveInsideRoot_RejectsEscapeOrExternalUri(string path)
    {
        Assert.Throws<InvalidDataException>(
            () => EpubPathResolver.ResolveInsideRoot(_root, path));
    }

    [Fact]
    public void ResolveInsideRoot_RejectsAbsolutePath()
    {
        var absolute = Path.Combine(Path.GetPathRoot(_root)!, "outside.xhtml");

        Assert.Throws<InvalidDataException>(
            () => EpubPathResolver.ResolveInsideRoot(_root, absolute));
    }

    [Fact]
    public void ResolverDentroRaiz_PermiteSegmentoPaiQuePermaneceNaObra()
    {
        var baseDirectory = Path.Combine(_root, "OEBPS", "Text");
        var result = EpubPathResolver.ResolveInsideRoot(
            _root,
            baseDirectory,
            "../Images/cover.jpg");

        Assert.True(EpubPathResolver.IsInsideRoot(_root, result));
    }
}
