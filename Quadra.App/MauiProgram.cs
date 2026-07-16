using Microsoft.Extensions.Logging;
using Quadra.App.Data;
using Quadra.App.Pages;
using Quadra.App.Services;
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
        builder.Services.AddSingleton<LibraryStorageService>();

#if ANDROID
        builder.Services.AddSingleton<
            IPdfCoverService,
            PdfCoverService>();

        builder.Services.AddSingleton<
            IPdfReaderService,
            PdfReaderService>();
#else
builder.Services.AddSingleton<
    IPdfReaderService,
    UnsupportedPdfReaderService>();
#endif

        builder.Services.AddSingleton<CoverService>();
        builder.Services.AddSingleton<ComicReaderService>();

        builder.Services.AddSingleton<
            IEpubReaderService,
            EpubReaderService>();

        builder.Services.AddSingleton<LibraryCleanupService>();

        builder.Services.AddTransient<LibraryViewModel>();
        builder.Services.AddTransient<LibraryPage>();

        builder.Services.AddTransient<BookDetailsViewModel>();
        builder.Services.AddTransient<BookDetailsPage>();

        builder.Services.AddTransient<ReaderViewModel>();
        builder.Services.AddTransient<ReaderPage>();

        builder.Services.AddTransient<EpubReaderPage>();
        builder.Services.AddTransient<EpubReaderViewModel>();
        return builder.Build();
    }
}
