using Microsoft.Maui.Storage;
using Quadra.App.Infrastructure;
using Quadra.App.Models;
using Quadra.App.Policies;
using Quadra.App.Services.Import;

namespace Quadra.App.Services.Storage;

public class ArmazenamentoBibliotecaService
{
    private readonly string _libraryDirectory;
    private readonly IEspacoArmazenamentoService _espacoArmazenamentoService;

    public ArmazenamentoBibliotecaService(IEspacoArmazenamentoService espacoArmazenamentoService)
    {
        _espacoArmazenamentoService = espacoArmazenamentoService;
        _libraryDirectory = Path.Combine(
            FileSystem.Current.AppDataDirectory,
            "Library");

        Directory.CreateDirectory(_libraryDirectory);
    }

    public async Task<ObraBiblioteca> ImportarAsync(
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

        PoliticaEspacoArmazenamento.GarantirDisponivel(
            _espacoArmazenamentoService,
            destinationPath,
            PoliticaEspacoArmazenamento.EstimarBytesImportacao(sourceLength ?? 0),
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
                            PoliticaEspacoArmazenamento.GarantirDisponivel(
                                _espacoArmazenamentoService,
                                destinationPath,
                                read,
                                "Não há espaço disponível suficiente para importar este arquivo.");
                        }

                        await outputStream.WriteAsync(
                            buffer.AsMemory(0, read),
                            cancellationToken);
                    }
                },
                partialPath => ValidacaoArquivoService.ValidarAsync(
                    partialPath,
                    extension,
                    cancellationToken),
                cancellationToken);
        }
        catch (IOException exception) when (
            exception is not EspacoArmazenamentoInsuficienteException)
        {
            throw new IOException(
                "Não foi possível gravar o arquivo no armazenamento interno.",
                exception);
        }

        var title = Path.GetFileNameWithoutExtension(file.FileName);

        return new ObraBiblioteca
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

    public Task ExcluirAsync(ObraBiblioteca item)
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
