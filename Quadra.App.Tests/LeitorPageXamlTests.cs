using System.Xml.Linq;

namespace Quadra.App.Tests;

public sealed class LeitorPageXamlTests
{
    private static readonly HashSet<string> SupportedSafeAreaRegions =
    [
        "Default",
        "None",
        "Container",
        "SoftInput",
        "All"
    ];

    [Fact]
    public void SafeAreaEdges_UseSupportedMauiRegions()
    {
        var xamlPath = Path.Combine(AppContext.BaseDirectory, "LeitorPage.xaml");
        var document = XDocument.Load(xamlPath);
        var values = document.Root!
            .DescendantsAndSelf()
            .Attributes("SafeAreaEdges")
            .SelectMany(attribute => attribute.Value.Split(',', StringSplitOptions.TrimEntries));

        Assert.All(values, value => Assert.Contains(value, SupportedSafeAreaRegions));
    }
}
