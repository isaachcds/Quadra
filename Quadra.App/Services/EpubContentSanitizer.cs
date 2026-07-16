using System.Text.RegularExpressions;

namespace Quadra.App.Services;

public static class EpubContentSanitizer
{
    private const string CssUrlPattern = "url\\(\\s*(?<quote>['\\\"]?)(?<value>.*?)(\\k<quote>)\\s*\\)";
    private const string CssImportPattern = "@import\\s+(?:url\\(\\s*)?(?<quote>['\\\"]?)(?<value>[^'\\\"\\)\\s;]+)(\\k<quote>)(?:\\s*\\))?";

    public static string SanitizeCssReferences(
        string content,
        string rootDirectory,
        string baseDirectory)
    {
        var sanitized = Regex.Replace(
            content,
            CssUrlPattern,
            match => IsSafeReference(
                rootDirectory,
                baseDirectory,
                match.Groups["value"].Value)
                ? match.Value
                : "url()",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return Regex.Replace(
            sanitized,
            CssImportPattern,
            match => IsSafeReference(
                rootDirectory,
                baseDirectory,
                match.Groups["value"].Value)
                ? match.Value
                : string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

    public static bool IsSafeReference(
        string rootDirectory,
        string baseDirectory,
        string reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.StartsWith('#'))
            return true;

        var pathOnly = reference.Split('#', '?')[0];

        try
        {
            EpubPathResolver.ResolveInsideRoot(
                rootDirectory,
                baseDirectory,
                pathOnly);
            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}
