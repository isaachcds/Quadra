using Microsoft.Extensions.Logging;
using Quadra.App.Data;
using Quadra.App.Pages;
using Quadra.App.Services;
using Quadra.App.ViewModels;

#if ANDROID
using Quadra.App.Platforms.Android.Services;
#endif

namespace Quadra.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

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

        builder.Services.AddTransient<LibraryViewModel>();
        builder.Services.AddTransient<LibraryPage>();

        builder.Services.AddTransient<BookDetailsViewModel>();
        builder.Services.AddTransient<BookDetailsPage>();

        builder.Services.AddTransient<ReaderViewModel>();
        builder.Services.AddTransient<ReaderPage>();

        return builder.Build();
    }
}