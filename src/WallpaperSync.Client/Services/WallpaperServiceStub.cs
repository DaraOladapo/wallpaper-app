using WallpaperSync.Contracts;

namespace WallpaperSync.Client.Services;

/// <summary>
/// Fallback stub – used when the real platform service is not registered.
/// Always returns false (no-op).
/// </summary>
internal sealed class WallpaperServiceStub : IWallpaperService
{
    public Task<bool> TryApplyWallpaperAsync(string localImagePath, CancellationToken ct)
        => Task.FromResult(false);
}
