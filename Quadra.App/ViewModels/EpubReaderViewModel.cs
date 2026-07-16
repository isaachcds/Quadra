using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quadra.App.Data;
using Quadra.App.Models;
using Quadra.App.Services;
using System.Collections.ObjectModel;

namespace Quadra.App.ViewModels;

public partial class EpubReaderViewModel
    : ObservableObject, IQueryAttributable
{
    private readonly IEpubReaderService _epubReaderService;
    private readonly QuadraDatabase _database;
    private CancellationTokenSource? _loadCancellation;

    public string? ContentRoot { get; private set; }

    public ObservableCollection<EpubChapter> Capitulos { get; } = [];

    [ObservableProperty]
    private LibraryItem? item;

    [ObservableProperty]
    private int capituloAtual;

    [ObservableProperty]
    private bool estaCarregando;

    [ObservableProperty]
    private bool controlesVisiveis = true;

    [ObservableProperty]
    private string textoCapitulo = string.Empty;

    [ObservableProperty]
    private string tituloCapitulo = string.Empty;

    [ObservableProperty]
    private WebViewSource? conteudoCapitulo;

    public EpubReaderViewModel(
        IEpubReaderService epubReaderService,
        QuadraDatabase database)
    {
        _epubReaderService = epubReaderService;
        _database = database;
    }

    public async void ApplyQueryAttributes(
        IDictionary<string, object> query)
    {
        if (!query.TryGetValue("Item", out var value))
            return;

        if (value is not LibraryItem libraryItem)
            return;

        Item = libraryItem;
        ContentRoot = _epubReaderService.GetContentRoot(libraryItem);
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();

        await CarregarLivroAsync(_loadCancellation.Token);
    }

    private async Task CarregarLivroAsync(CancellationToken cancellationToken)
    {
        if (Item is null)
            return;

        try
        {
            EstaCarregando = true;

            var capitulos =
                await _epubReaderService.LoadChaptersAsync(
                    Item,
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            Capitulos.Clear();

            foreach (var capitulo in capitulos)
                Capitulos.Add(capitulo);

            CapituloAtual = Math.Clamp(
                Item.CurrentPage,
                0,
                Math.Max(0, Capitulos.Count - 1));

            await CarregarCapituloAtualAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A página deixou de precisar deste carregamento.
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Erro ao abrir EPUB",
                ex.Message,
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            EstaCarregando = false;
        }
    }

    private async Task CarregarCapituloAtualAsync(
        CancellationToken cancellationToken = default)
    {
        if (Capitulos.Count == 0)
            return;

        var indice = Math.Clamp(
            CapituloAtual,
            0,
            Capitulos.Count - 1);

        var capitulo = Capitulos[indice];

        if (!File.Exists(capitulo.LocalFilePath))
        {
            throw new FileNotFoundException(
                "O arquivo do capítulo não foi encontrado.",
                capitulo.LocalFilePath);
        }

        var html = await File.ReadAllTextAsync(
            capitulo.LocalFilePath,
            cancellationToken);

        html = SanitizarReferenciasLocais(
            html,
            capitulo.LocalFilePath);
        html = AplicarEstiloLeitura(html);

        var directory =
            Path.GetDirectoryName(capitulo.LocalFilePath);

        ConteudoCapitulo = new HtmlWebViewSource
        {
            Html = html,
            BaseUrl = directory
        };

        TituloCapitulo = capitulo.Title;

        AtualizarTextoCapitulo();

        await SalvarProgressoAsync();
    }

    private string SanitizarReferenciasLocais(
        string html,
        string chapterPath)
    {
        if (string.IsNullOrWhiteSpace(ContentRoot))
            return html;

        var chapterDirectory = Path.GetDirectoryName(chapterPath) ?? ContentRoot;

        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            html,
            "(?<attribute>src|href)\\s*=\\s*(?<quote>[\\\"'])(?<value>.*?)(\\k<quote>)",
            match =>
            {
                var value = match.Groups["value"].Value;

                if (string.IsNullOrWhiteSpace(value) || value.StartsWith('#'))
                    return match.Value;

                if (EpubContentSanitizer.IsSafeReference(
                        ContentRoot,
                        chapterDirectory,
                        value))
                {
                    return match.Value;
                }

                return $"{match.Groups["attribute"].Value}=\"#\"";
            },
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Singleline);

        return EpubContentSanitizer.SanitizeCssReferences(
            sanitized,
            ContentRoot,
            chapterDirectory);
    }

    public bool IsLocalNavigationAllowed(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.StartsWith('#'))
            return true;

        if (url.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(ContentRoot) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !uri.IsFile)
        {
            return false;
        }

        return EpubPathResolver.IsInsideRoot(ContentRoot, uri.LocalPath);
    }

    public void CancelOperations()
    {
        _loadCancellation?.Cancel();
    }

    private void AtualizarTextoCapitulo()
    {
        TextoCapitulo = Capitulos.Count == 0
            ? string.Empty
            : $"{CapituloAtual + 1} / {Capitulos.Count}";
    }

    private async Task SalvarProgressoAsync()
    {
        if (Item is null || Capitulos.Count == 0)
            return;

        Item.CurrentPage = CapituloAtual;
        Item.TotalPages = Capitulos.Count;
        Item.LastReadAt = DateTime.Now;

        await _database.SaveLibraryItemAsync(Item);
    }

    [RelayCommand]
    private async Task AvancarCapituloAsync()
    {
        if (Capitulos.Count == 0)
            return;

        if (CapituloAtual >= Capitulos.Count - 1)
            return;

        CapituloAtual++;

        await CarregarCapituloAtualAsync();
    }

    [RelayCommand]
    private async Task VoltarCapituloAsync()
    {
        if (Capitulos.Count == 0)
            return;

        if (CapituloAtual <= 0)
            return;

        CapituloAtual--;

        await CarregarCapituloAtualAsync();
    }

    private static string AplicarEstiloLeitura(string html)
    {
        const string readerStyle = """
        <style id="quadra-reader-style">
            :root {
                color-scheme: light;
            }

            html {
                background-color: #FAF8F3 !important;
            }

            body {
                box-sizing: border-box;
                max-width: 760px;
                margin: 0 auto !important;
                padding: 24px 20px 48px 20px !important;

                background-color: #FAF8F3 !important;
                color: #242424 !important;

                font-family:
                    -apple-system,
                    BlinkMacSystemFont,
                    "Segoe UI",
                    Roboto,
                    Arial,
                    sans-serif !important;

                font-size: 18px !important;
                line-height: 1.7 !important;

                overflow-wrap: break-word;
                word-wrap: break-word;
            }

            p {
                margin-top: 0;
                margin-bottom: 1em;
            }

            h1,
            h2,
            h3,
            h4,
            h5,
            h6 {
                color: #202020 !important;
                line-height: 1.3 !important;
                margin-top: 1.4em;
                margin-bottom: 0.7em;
            }

            img,
            svg,
            video {
                display: block;
                max-width: 100% !important;
                height: auto !important;
                margin-left: auto;
                margin-right: auto;
            }

            table {
                display: block;
                max-width: 100%;
                overflow-x: auto;
                border-collapse: collapse;
            }

            pre,
            code {
                white-space: pre-wrap;
                overflow-wrap: break-word;
            }

            a {
                color: #5B3FE5 !important;
            }

            blockquote {
                margin-left: 0;
                padding-left: 16px;
                border-left: 3px solid #C8BDF8;
            }
        </style>
        """;

        if (html.Contains(
            "quadra-reader-style",
            StringComparison.OrdinalIgnoreCase))
        {
            return html;
        }

        var headClosingIndex = html.IndexOf(
            "</head>",
            StringComparison.OrdinalIgnoreCase);

        if (headClosingIndex >= 0)
        {
            return html.Insert(
                headClosingIndex,
                readerStyle);
        }

        var bodyOpeningIndex = html.IndexOf(
            "<body",
            StringComparison.OrdinalIgnoreCase);

        if (bodyOpeningIndex >= 0)
        {
            return html.Insert(
                bodyOpeningIndex,
                $"<head>{readerStyle}</head>");
        }

        return $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta
                name="viewport"
                content="width=device-width, initial-scale=1.0" />

            {readerStyle}
        </head>
        <body>
            {html}
        </body>
        </html>
        """;
    }

    [RelayCommand]
    private void AlternarControles()
    {
        ControlesVisiveis = !ControlesVisiveis;
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
