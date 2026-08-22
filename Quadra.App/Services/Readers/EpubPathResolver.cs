using Quadra.App.Policies;

namespace Quadra.App.Services.Readers;

public static class EpubPathResolver
{
    public static string ResolveInsideRoot(
        string rootDirectory,
        string relativePath)
    {
        return ResolveInsideRoot(rootDirectory, rootDirectory, relativePath);
    }

    public static string ResolveInsideRoot(
        string rootDirectory,
        string baseDirectory,
        string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var decoded = Uri.UnescapeDataString(relativePath);

        if (Uri.TryCreate(decoded, UriKind.Absolute, out _))
        {
            throw new InvalidDataException(
                "O EPUB contém uma referência externa ou absoluta não permitida.");
        }

        decoded = decoded
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(decoded))
            throw new InvalidDataException("O EPUB contém um caminho absoluto não permitido.");

        if (decoded.Length > FileProcessingLimits.MaximumPathLength)
            throw new InvalidDataException("O EPUB contém um caminho excessivamente longo.");

        var root = Path.GetFullPath(rootDirectory)
            .TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        var safeBase = Path.GetFullPath(baseDirectory);

        if (!IsInsideRoot(rootDirectory, safeBase) &&
            !safeBase.TrimEnd(Path.DirectorySeparatorChar)
                .Equals(
                    Path.GetFullPath(rootDirectory)
                        .TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("A pasta base está fora da obra.");
        }

        var resolved = Path.GetFullPath(Path.Combine(safeBase, decoded));

        if (!resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("O EPUB contém um caminho fora da pasta da obra.");

        return resolved;
    }

    public static bool IsInsideRoot(string rootDirectory, string path)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory) ||
            string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var root = Path.GetFullPath(rootDirectory)
            .TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        var resolved = Path.GetFullPath(path);

        return resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }
}
