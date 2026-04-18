using Microsoft.Extensions.Logging;
using WallpaperSync.Client.Services;
using WallpaperSync.Contracts;

namespace WallpaperSync.Client;

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
            });

        builder.Services.AddHttpClient();

        // Platform-specific WallpaperService is registered via DI in each platform's
        // partial MauiProgram (see Platforms/*).
        // For platforms where it is not overridden, a no-op stub is used.

        builder.Services.AddSingleton<IWallpaperService, WallpaperServiceStub>();

        builder.Services.AddSingleton<WallpaperSyncClient>(sp =>
        {
            // Hub URL is read directly from environment/configuration at startup.
            // In production, set WALLPAPERSYNC__HUBURL in your App Service / container config.
            var hubUrl = Environment.GetEnvironmentVariable("WALLPAPERSYNC__HUBURL")
                         ?? throw new InvalidOperationException(
                             "WALLPAPERSYNC__HUBURL environment variable is not set.");
            return new WallpaperSyncClient(
                hubUrl,
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
                sp.GetRequiredService<IWallpaperService>(),
                sp.GetRequiredService<ILogger<WallpaperSyncClient>>());
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
