using Microsoft.Extensions.Logging;
using Quadra.App.Data;
using Quadra.App.Pages;
using Quadra.App.Services.Covers;
using Quadra.App.Services.Import;
using Quadra.App.Services.Readers;
using Quadra.App.Services.Storage;
using Quadra.App.ViewModels;

#if ANDROID
using Quadra.App.Platforms.Android.Services;
using Microsoft.Maui.Handlers;
#endif

namespace Quadra.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

#if ANDROID
        WebViewHandler.Mapper.AppendToMapping(
            "QuadraDisableEpubJavaScript",
            (handler, _) =>
            {
                handler.PlatformView.Settings.JavaScriptEnabled = false;
            });
#endif

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont(
                    "OpenSans-Regular.ttf",
                    "OpenSansRegular");

                fonts.AddFont(
                    "OpenSans-Semibold.ttf",
                    "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<QuadraDatabase>();
        builder.Services.AddSingleton<ArmazenamentoBibliotecaService>();

        builder.Services.AddSingleton<ImportacaoBibliotecaService>();
#if ANDROID
        builder.Services.AddSingleton<
            IEspacoArmazenamentoService,
            EspacoArmazenamentoAndroidService>();

        builder.Services.AddSingleton<
            ICapaPdfService,
            CapaPdfService>();

        builder.Services.AddSingleton<
            ILeitorPdfService,
            LeitorPdfService>();
        builder.Services.AddSingleton<AbrirComAndroidService>();
#else
builder.Services.AddSingleton<
    IEspacoArmazenamentoService,
    EspacoArmazenamentoService>();

builder.Services.AddSingleton<
    ILeitorPdfService,
    LeitorPdfNaoSuportadoService>();
#endif

        builder.Services.AddSingleton<CapaService>();
        builder.Services.AddSingleton<LeitorQuadrinhosService>();

        builder.Services.AddSingleton<
            ILeitorEpubService,
            LeitorEpubService>();

        builder.Services.AddSingleton<LimpezaBibliotecaService>();
        builder.Services.AddSingleton<DiagnosticoArmazenamentoService>();

        builder.Services.AddTransient<BibliotecaViewModel>();
        builder.Services.AddTransient<BibliotecaPage>();
        builder.Services.AddTransient<ColecoesPage>();
        builder.Services.AddTransient<ColecoesViewModel>();
        builder.Services.AddTransient<DetalhesColecaoViewModel>();
        builder.Services.AddTransient<DetalhesColecaoPage>();
        builder.Services.AddTransient<HistoricoPage>();
        builder.Services.AddTransient<HistoricoViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<ConfiguracoesViewModel>();

        builder.Services.AddTransient<DetalhesObraViewModel>();
        builder.Services.AddTransient<DetalhesObraPage>();

        builder.Services.AddTransient<LeitorViewModel>();
        builder.Services.AddTransient<LeitorPage>();

        builder.Services.AddTransient<LeitorEpubPage>();
        builder.Services.AddTransient<LeitorEpubViewModel>();
        return builder.Build();
    }
}
