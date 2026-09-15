# Global Wallpaper Sync (Cloud-Native, .NET)

This repository now captures a minimal, implementation-ready blueprint for a **cross-platform wallpaper sync app** using your requested stack:

- **Client Apps:** .NET MAUI (Windows, MacCatalyst, Android, iOS)
- **Backend:** Azure Functions + Azure Blob Storage + Azure SignalR Service

## Architecture

```text
Sync Portal Upload
      |
      v
Azure Function (HTTP Trigger)
  - validates image
  - writes image to Blob Storage
  - broadcasts WallpaperChanged event via SignalR
      |
      v
MAUI Clients (connected to SignalR hub)
  - Windows/Mac/Android: auto-download + set wallpaper
  - iOS: receives event + stores image for user-assisted Shortcut flow
```

## Platform Capability Matrix

| Platform | Programmatic wallpaper set | Strategy |
|---|---|---|
| Windows | Yes | Win32 `SystemParametersInfo` |
| macOS (MacCatalyst host) | Yes (bridge) | `osascript` AppleScript bridge |
| Android | Yes | `WallpaperManager` |
| iOS/iPadOS | No (fully automatic blocked) | Save image + trigger user Shortcut workflow |

## Minimal SignalR Client (Background Sync)

Below is a production-style baseline for the **client-side SignalR listener** you asked for.  
It is platform-agnostic and delegates OS-specific wallpaper logic to `IWallpaperService`.

```csharp
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

public sealed record WallpaperChangedEvent(string ImageUrl, string CorrelationId);

public interface IWallpaperService
{
    Task<bool> TryApplyWallpaperAsync(string localImagePath, CancellationToken ct);
}

public sealed class WallpaperSyncClient : IAsyncDisposable
{
    private readonly HubConnection _hub;
    private readonly HttpClient _httpClient;
    private readonly IWallpaperService _wallpaperService;
    private readonly ILogger<WallpaperSyncClient> _logger;

    public WallpaperSyncClient(
        string hubUrl,
        HttpClient httpClient,
        IWallpaperService wallpaperService,
        ILogger<WallpaperSyncClient> logger)
    {
        _httpClient = httpClient;
        _wallpaperService = wallpaperService;
        _logger = logger;

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hub.On<WallpaperChangedEvent>("WallpaperChanged", HandleWallpaperChangedAsync);
    }

    public Task StartAsync(CancellationToken ct) => _hub.StartAsync(ct);
    public Task StopAsync(CancellationToken ct) => _hub.StopAsync(ct);

    private async Task HandleWallpaperChangedAsync(WallpaperChangedEvent evt)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var imageBytes = await _httpClient.GetByteArrayAsync(evt.ImageUrl, cts.Token);

            var extension = Path.GetExtension(new Uri(evt.ImageUrl).AbsolutePath);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            var localPath = Path.Combine(FileSystem.CacheDirectory, $"wallpaper-{evt.CorrelationId}{extension}");
            await File.WriteAllBytesAsync(localPath, imageBytes, cts.Token);

            var applied = await _wallpaperService.TryApplyWallpaperAsync(localPath, cts.Token);
            if (!applied)
            {
                _logger.LogInformation(
                    "Wallpaper downloaded but not auto-applied (likely iOS policy). CorrelationId={CorrelationId}",
                    evt.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wallpaper sync failed.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hub.DisposeAsync();
    }
}
```

## OS-Specific Wallpaper Service Notes

- **Windows:** call `SystemParametersInfo(SPI_SETDESKWALLPAPER, ...)` via P/Invoke.
- **macOS:** run `osascript` command to set Finder desktop picture.
- **Android:** use `WallpaperManager.SetBitmap(...)`.
- **iOS:** return `false` from `TryApplyWallpaperAsync(...)` and route user to Shortcut-assisted flow.

## Azure Function Event Contract (recommended)

When an image upload succeeds, broadcast:

```json
{
  "event": "WallpaperChanged",
  "imageUrl": "https://<storage>/wallpapers/<id>.jpg",
  "correlationId": "<guid>"
}
```

## DevOps Notes

- Put SignalR hub URL and storage container settings in environment configuration.
- Use managed identity for Function-to-Storage access.
- Add retry/backoff and idempotency checks around downloads on clients.
- Log with `correlationId` end-to-end for troubleshooting.

---

If useful, the next step can be scaffolding this into a real MAUI solution layout (`Client`, `Functions`, shared contracts) with compile-ready platform implementations.
