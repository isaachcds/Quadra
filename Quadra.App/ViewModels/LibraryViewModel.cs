using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quadra.App.Data;
using Quadra.App.Models;
using Quadra.App.Pages;
using Quadra.App.Services;
using System.Collections.ObjectModel;

namespace Quadra.App.ViewModels;

public partial class LibraryViewModel : ObservableObject
{
    private readonly LibraryStorageService _storageService;
    private readonly QuadraDatabase _database;
    private readonly CoverService _coverService;
    private readonly LibraryCleanupService _cleanupService;

    public ObservableCollection<LibraryItem> Itens { get; } = [];

    public LibraryViewModel(
    LibraryStorageService storageService,
    QuadraDatabase database,
    CoverService coverService,
    LibraryCleanupService cleanupService)
    {
        _storageService = storageService;
        _database = database;
        _coverService = coverService;
        _cleanupService = cleanupService;
    }

    [ObservableProperty]
    private string? nomeArquivoSelecionado;

    [ObservableProperty]
    private bool possuiArquivoSelecionado;

    [ObservableProperty]
    private bool estaImportando;

    [ObservableProperty]
    private LibraryItem? itemImportado;

    [ObservableProperty]
    private bool bibliotecaVazia = true;

    [ObservableProperty]
    private bool possuiItens;

    [RelayCommand]
    private async Task ImportarArquivoAsync()
    {
        if (EstaImportando)
            return;

        try
        {
            EstaImportando = true;

            var tiposPermitidos = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    {
                        DevicePlatform.Android,
                        new[]
                        {
                            "*/*"
                        }
                    },
                   {
                    DevicePlatform.WinUI,
                    new[]
                    {
                        ".pdf",
                        ".cbz",
                        ".cbr",
                        ".epub"
                    }
                }
                });

            var opcoes = new PickOptions
            {
                PickerTitle = "Selecione um CBR, CBZ, PDF ou EPUB",
                FileTypes = tiposPermitidos
            };

            var arquivo = await FilePicker.Default.PickAsync(opcoes);

            if (arquivo is null)
                return;

            var extensao = Path
                .GetExtension(arquivo.FileName)
                .ToLowerInvariant();

            string[] extensoesPermitidas =
            [
                ".cbr",
                ".cbz",
                ".pdf",
                ".epub"
            ];

            if (!extensoesPermitidas.Contains(extensao))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Arquivo não suportado",
                    "Escolha um arquivo CBR, CBZ, PDF ou EPUB.",
                    "Entendi");

                return;
            }

            ItemImportado = await _storageService.ImportAsync(arquivo);

            ItemImportado.CoverPath =
                await _coverService.GenerateCoverAsync(ItemImportado);

            await _database.SaveLibraryItemAsync(ItemImportado);

            Itens.Insert(0, ItemImportado);

            AtualizarEstadoBiblioteca();

            NomeArquivoSelecionado = ItemImportado.OriginalFileName;
            PossuiArquivoSelecionado = true;

            await Shell.Current.DisplayAlertAsync(
                "Importação concluída",
                $"{ItemImportado.Title} foi copiado para a biblioteca do Quadra.",
                "OK");
        }
        catch (TaskCanceledException)
        {
            // O usuário cancelou a operação.
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Erro ao importar",
                ex.Message,
                "OK");
        }
        finally
        {
            EstaImportando = false;
        }
    }

    [RelayCommand]
    private async Task ExcluirItemAsync(LibraryItem? item)
    {
        if (item is null)
            return;

        var confirmou = await Shell.Current.DisplayAlertAsync(
            "Remover obra",
            $"Deseja remover \"{item.Title}\" da biblioteca do Quadra?",
            "Remover",
            "Cancelar");

        if (!confirmou)
            return;

        try
        {
            await _cleanupService.DeleteFilesAsync(item);
            await _database.DeleteLibraryItemAsync(item);

            Itens.Remove(item);

            AtualizarEstadoBiblioteca();

            await Shell.Current.DisplayAlertAsync(
                "Obra removida",
                "A cópia interna, a capa e os arquivos temporários foram removidos. O arquivo original não foi apagado.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Erro ao remover",
                ex.Message,
                "OK");
        }
    }

    [RelayCommand]
    private async Task CarregarBibliotecaAsync()
    {
        try
        {
            var itensSalvos = await _database.GetLibraryItemsAsync();

            Itens.Clear();

            foreach (var item in itensSalvos)
                Itens.Add(item);

            AtualizarEstadoBiblioteca();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Erro ao carregar biblioteca",
                ex.Message,
                "OK");
        }
    }

    [RelayCommand]
    private async Task AbrirDetalhesAsync(
    LibraryItem? item)
    {
        if (item is null)
            return;

        var parametros = new Dictionary<string, object>
        {
            ["Item"] = item
        };

        await Shell.Current.GoToAsync(
            nameof(BookDetailsPage),
            parametros);
    }


    //metodos privados
    private void AtualizarEstadoBiblioteca()
    {
        PossuiItens = Itens.Count > 0;
        BibliotecaVazia = !PossuiItens;
    }
}