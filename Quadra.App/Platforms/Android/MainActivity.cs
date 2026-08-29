using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Quadra.App.Platforms.Android.Services;

namespace Quadra.App
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter([Android.Content.Intent.ActionView], Categories = [Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable], DataSchemes = ["content", "file"], DataMimeTypes = ["application/pdf", "application/epub+zip", "application/zip", "application/octet-stream"])]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _ = EncaminharIntentAsync(Intent);
        }

        protected override void OnNewIntent(Android.Content.Intent? intent)
        {
            base.OnNewIntent(intent);
            Intent = intent;
            _ = EncaminharIntentAsync(intent);
        }

        private static Task EncaminharIntentAsync(Android.Content.Intent? intent)
        {
            var platformApplication = IPlatformApplication.Current;
            return platformApplication is null
                ? Task.CompletedTask
                : platformApplication.Services
                    .GetRequiredService<AbrirComAndroidService>()
                    .ProcessarAsync(intent);
        }
    }
}
