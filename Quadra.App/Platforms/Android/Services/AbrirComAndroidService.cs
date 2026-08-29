using Android.Content;
using Android.Provider;
using Microsoft.Extensions.DependencyInjection;
using Quadra.App.Pages;
using Quadra.App.Services.Import;

namespace Quadra.App.Platforms.Android.Services;

public sealed class AbrirComAndroidService
{
    private readonly ImportacaoBibliotecaService _importacao;
    private readonly HashSet<string> _intentsProcessados = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public AbrirComAndroidService(ImportacaoBibliotecaService importacao) => _importacao = importacao;

    public async Task ProcessarAsync(Intent? intent)
    {
        if (intent?.Action != Intent.ActionView || intent.Data is not { } uri)
            return;

        var chave = $"{intent.Action}|{uri}";
        lock (_gate) { if (!_intentsProcessados.Add(chave)) return; }

        try
        {
            var resolver = Platform.AppContext?.ContentResolver;
            if (resolver is null) return;
            var nome = ObterNome(resolver, uri) ?? uri.LastPathSegment ?? "arquivo";
            var extensao = Path.GetExtension(nome).ToLowerInvariant();
            if (!SupportedFileFormats.IsSupported(extensao))
            {
                await Shell.Current.DisplayAlertAsync("Arquivo não suportado", "Escolha um arquivo CBR, CBZ, PDF ou EPUB.", "Entendi");
                return;
            }

            var obra = await _importacao.ImportarConteudoAsync(nome, token => Task.FromResult<Stream>(resolver.OpenInputStream(uri) ?? throw new IOException()), CancellationToken.None);
            await Shell.Current.GoToAsync("BookDetailsPage", new Dictionary<string, object> { ["Item"] = obra });
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro ao importar", "Não foi possível importar este arquivo.", "OK");
        }
    }

    private static string? ObterNome(ContentResolver resolver, global::Android.Net.Uri uri)
    {
        if (uri.Scheme != ContentResolver.SchemeContent) return uri.LastPathSegment;
        using var cursor = resolver.Query(uri, [IOpenableColumns.DisplayName], null, null, null);
        if (cursor is null || !cursor.MoveToFirst()) return null;
        var indice = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
        return indice >= 0 ? cursor.GetString(indice) : null;
    }
}
