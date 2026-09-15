namespace WallpaperSync.Contracts;

/// <summary>
/// Event broadcast by Azure SignalR when a new wallpaper image is ready.
/// </summary>
public sealed record WallpaperChangedEvent(string ImageUrl, string CorrelationId);
