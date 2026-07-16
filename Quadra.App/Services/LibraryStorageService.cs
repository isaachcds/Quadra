using Microsoft.Maui.Storage;
using Quadra.App.Models;

namespace Quadra.App.Services;

public class LibraryStorageService
{
    private readonly string _libraryDirectory;
    private readonly IStorageSpaceService _storageSpaceService;

    public LibraryStorageService(IStorageSpaceService storageSpaceService)
    {
        _storageSpaceService = storageSpaceService;
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

        if (!SupportedFileFormats.IsSupported(extension))
        {
            throw new InvalidOperationException(
                "O formato do arquivo não é suportado.");
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension}";

        var destinationPath = Path.Combine(
            _libraryDirectory,
            storedFileName);

        await using var inputStream = await file.OpenReadAsync();
        var sourceLength = TryGetLength(inputStream);

        if (sourceLength > FileProcessingLimits.MaximumImportBytes)
        {
            throw new InvalidDataException(
                "Este arquivo excede o limite de processamento configurado pelo Quadra.");
        }

        StorageSpacePolicy.EnsureAvailable(
            _storageSpaceService,
            destinationPath,
            StorageSpacePolicy.EstimateImportBytes(sourceLength ?? 0),
            "Não há espaço disponível suficiente para importar este arquivo.");

        try
        {
            await AtomicFile.WriteAsync(
                destinationPath,
                async outputStream =>
                {
                    var buffer = new byte[81920];
                    long totalBytes = 0;

                    while (true)
                    {
                        var read = await inputStream.ReadAsync(
                            buffer,
                            cancellationToken);

                        if (read == 0)
                            break;

                        totalBytes += read;

                        if (totalBytes > FileProcessingLimits.MaximumImportBytes)
                        {
                            throw new InvalidDataException(
                                "Este arquivo excede o limite de processamento configurado pelo Quadra.");
                        }

                        if (!sourceLength.HasValue)
                        {
                            StorageSpacePolicy.EnsureAvailable(
                                _storageSpaceService,
                                destinationPath,
                                read,
                                "Não há espaço disponível suficiente para importar este arquivo.");
                        }

                        await outputStream.WriteAsync(
                            buffer.AsMemory(0, read),
                            cancellationToken);
                    }
                },
                partialPath => FileValidationService.ValidateAsync(
                    partialPath,
                    extension,
                    cancellationToken),
                cancellationToken);
        }
        catch (IOException exception) when (
            exception is not InsufficientStorageException)
        {
            throw new IOException(
                "Não foi possível gravar o arquivo no armazenamento interno.",
                exception);
        }

        var title = Path.GetFileNameWithoutExtension(file.FileName);

        return new LibraryItem
        {
            Title = title,
            OriginalFileName = file.FileName,
            StoredFileName = storedFileName,
            FilePath = destinationPath,
            Format = SupportedFileFormats.NormalizeFormat(extension),
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

    private static long? TryGetLength(Stream stream)
    {
        try
        {
            return stream.CanSeek
                ? Math.Max(0, stream.Length - stream.Position)
                : null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}
