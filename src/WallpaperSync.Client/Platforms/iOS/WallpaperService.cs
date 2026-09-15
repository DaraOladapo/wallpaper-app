using WallpaperSync.Contracts;

namespace WallpaperSync.Client.Platforms.iOS;

/// <summary>
/// iOS wallpaper service.
/// iOS/iPadOS does NOT allow programmatic wallpaper changes by third-party apps.
///
/// Fallback: the downloaded image is saved to a well-known folder so the user
/// can invoke a Siri Shortcut ("On Folder Update → Set Wallpaper").
/// Returns <c>false</c> to signal that user interaction is required.
/// </summary>
public sealed class WallpaperService : IWallpaperService
{
    /// <summary>
    /// Sub-folder inside the app's Documents directory that iCloud Drive can monitor.
    /// The user's Siri Shortcut should watch this folder.
    /// </summary>
    private const string SyncFolder = "WallpaperSync";

    public async Task<bool> TryApplyWallpaperAsync(string localImagePath, CancellationToken ct)
    {
        var destDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            SyncFolder);
        Directory.CreateDirectory(destDir);

        var destPath = Path.Combine(destDir, Path.GetFileName(localImagePath));
        await using var src = File.OpenRead(localImagePath);
        await using var dst = File.Create(destPath);
        await src.CopyToAsync(dst, ct);

        // Cannot set wallpaper programmatically on iOS.
        // Returning false signals the caller to show a user prompt / notification.
        return false;
    }
}
