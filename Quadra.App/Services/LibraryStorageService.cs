using Microsoft.Maui.Storage;
using Quadra.App.Models;

namespace Quadra.App.Services;

public class LibraryStorageService
{
    private readonly string _libraryDirectory;

    public LibraryStorageService()
    {
        _libraryDirectory = Path.Combine(
            FileSystem.Current.AppDataDirectory,
            "Library");

        Directory.CreateDirectory(_libraryDirectory);
    }

    public async Task<LibraryItem> ImportAsync(
        FileResult file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var extension = Path
            .GetExtension(file.FileName)
            .ToLowerInvariant();

        string[] allowedExtensions =
        [
            ".cbr",
            ".cbz",
            ".pdf"
        ];

        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                "O formato do arquivo não é suportado.");
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension}";

        var destinationPath = Path.Combine(
            _libraryDirectory,
            storedFileName);

        await using var inputStream = await file.OpenReadAsync();

        await using var outputStream = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await inputStream.CopyToAsync(
            outputStream,
            cancellationToken);

        var title = Path.GetFileNameWithoutExtension(file.FileName);

        return new LibraryItem
        {
            Title = title,
            OriginalFileName = file.FileName,
            StoredFileName = storedFileName,
            FilePath = destinationPath,
            Format = extension.TrimStart('.').ToUpperInvariant(),
            CurrentPage = 0,
            TotalPages = 0,
            ImportedAt = DateTime.Now
        };
    }

    public Task DeleteAsync(LibraryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!string.IsNullOrWhiteSpace(item.FilePath) &&
            File.Exists(item.FilePath))
        {
            File.Delete(item.FilePath);
        }

        if (!string.IsNullOrWhiteSpace(item.CoverPath) &&
            File.Exists(item.CoverPath))
        {
            File.Delete(item.CoverPath);
        }

        return Task.CompletedTask;
    }
}