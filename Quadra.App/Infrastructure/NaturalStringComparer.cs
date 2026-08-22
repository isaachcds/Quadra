using System.Text.RegularExpressions;

namespace Quadra.App.Infrastructure;

public sealed partial class NaturalStringComparer
    : IComparer<string>
{
    public static NaturalStringComparer Instance { get; } =
        new();

    private NaturalStringComparer()
    {
    }

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return 1;

        var xParts = NumberRegex().Split(x);
        var yParts = NumberRegex().Split(y);

        var count = Math.Min(
            xParts.Length,
            yParts.Length);

        for (var index = 0; index < count; index++)
        {
            var xPart = xParts[index];
            var yPart = yParts[index];

            var xIsNumber = long.TryParse(
                xPart,
                out var xNumber);

            var yIsNumber = long.TryParse(
                yPart,
                out var yNumber);

            int comparison;

            if (xIsNumber && yIsNumber)
            {
                comparison = xNumber.CompareTo(yNumber);
            }
            else
            {
                comparison = string.Compare(
                    xPart,
                    yPart,
                    StringComparison.OrdinalIgnoreCase);
            }

            if (comparison != 0)
                return comparison;
        }

        return xParts.Length.CompareTo(yParts.Length);
    }

    [GeneratedRegex("(\\d+)")]
    private static partial Regex NumberRegex();
}
