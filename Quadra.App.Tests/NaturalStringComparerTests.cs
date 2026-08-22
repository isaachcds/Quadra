using Quadra.App.Infrastructure;

namespace Quadra.App.Tests;

public class NaturalStringComparerTests
{
    [Theory]
    [InlineData("página1.jpg", "página2.jpg")]
    [InlineData("página2.jpg", "página10.jpg")]
    [InlineData("página01.jpg", "página2.jpg")]
    [InlineData("pagina2.jpg", "pagina2.png")]
    [InlineData("Pagina2.JPG", "pagina10.jpg")]
    public void Compare_OrdersNaturally(string first, string second)
    {
        Assert.True(NaturalStringComparer.Instance.Compare(first, second) < 0);
    }

    [Fact]
    public void Compare_IgnoresCaseForEquivalentNames()
    {
        Assert.Equal(
            0,
            NaturalStringComparer.Instance.Compare("PAGINA1.JPG", "pagina1.jpg"));
    }
}
