using Microsoft.Extensions.Logging;
using Quadra.App.Pages;
using Quadra.App.ViewModels;
using Quadra.App.Services;
using Quadra.App.Data;

namespace Quadra.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<QuadraDatabase>();
            builder.Services.AddSingleton<LibraryStorageService>();
            builder.Services.AddSingleton<CoverService>();

            builder.Services.AddTransient<LibraryViewModel>();
            builder.Services.AddTransient<LibraryPage>();

            builder.Services.AddTransient<BookDetailsViewModel>();
            builder.Services.AddTransient<BookDetailsPage>();
            return builder.Build();
        }
    }
}
