namespace WallpaperSync.Contracts;

/// <summary>
/// Platform-specific contract for applying a wallpaper image.
/// Implementations live in WallpaperSync.Client per-platform folders.
/// </summary>
public interface IWallpaperService
{
    /// <summary>
    /// Attempts to set the device wallpaper from a local file path.
    /// Returns <c>true</c> when the wallpaper was applied automatically;
    /// <c>false</c> when user interaction is required (e.g. iOS).
    /// </summary>
    Task<bool> TryApplyWallpaperAsync(string localImagePath, CancellationToken ct);
}
