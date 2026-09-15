using Android.App;
using Android.Graphics;
using Android.Views;
using WallpaperSync.Contracts;

namespace WallpaperSync.Client.Platforms.Android;

/// <summary>
/// Android wallpaper service using the platform WallpaperManager API.
/// Requires SET_WALLPAPER permission in AndroidManifest.xml.
/// </summary>
public sealed class WallpaperService : IWallpaperService
{
    public async Task<bool> TryApplyWallpaperAsync(string localImagePath, CancellationToken ct)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity
                       ?? throw new InvalidOperationException("CurrentActivity is null.");

        await using var stream = File.OpenRead(localImagePath);
        var bitmap = await BitmapFactory.DecodeStreamAsync(stream)
                     ?? throw new InvalidOperationException($"Failed to decode bitmap from {localImagePath}.");

        var manager = WallpaperManager.GetInstance(activity)
                      ?? throw new InvalidOperationException("WallpaperManager unavailable.");

        // Use WindowMetrics for API 30+ to get display size; fall back to DisplayMetrics for older versions.
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var windowManager = activity.GetSystemService(Context.WindowService) as IWindowManager;
            var metrics = windowManager?.CurrentWindowMetrics;
            if (metrics is not null)
            {
                manager.SuggestDesiredDimensions(metrics.Bounds.Width(), metrics.Bounds.Height());
            }
        }
        else
        {
#pragma warning disable CA1422 // DefaultDisplay is deprecated in API 30+
            var display = (activity.GetSystemService(Context.WindowService) as IWindowManager)?.DefaultDisplay;
            if (display is not null)
            {
                var displayMetrics = new DisplayMetrics();
                display.GetMetrics(displayMetrics);
                manager.SuggestDesiredDimensions(displayMetrics.WidthPixels, displayMetrics.HeightPixels);
            }
#pragma warning restore CA1422
        }

        manager.SetBitmap(bitmap);
        return true;
    }
}
