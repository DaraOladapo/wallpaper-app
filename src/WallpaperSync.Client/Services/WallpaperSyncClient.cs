using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using WallpaperSync.Contracts;

namespace WallpaperSync.Client.Services;

/// <summary>
/// Connects to the Azure SignalR hub and applies wallpaper changes in real time.
/// </summary>
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
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _wallpaperService = wallpaperService ?? throw new ArgumentNullException(nameof(wallpaperService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl ?? throw new ArgumentNullException(nameof(hubUrl)))
            .WithAutomaticReconnect()
            .Build();

        _hub.On<WallpaperChangedEvent>("WallpaperChanged", HandleWallpaperChangedAsync);
    }

    public Task StartAsync(CancellationToken ct = default) => _hub.StartAsync(ct);
    public Task StopAsync(CancellationToken ct = default) => _hub.StopAsync(ct);

    public HubConnectionState State => _hub.State;

    private async Task HandleWallpaperChangedAsync(WallpaperChangedEvent evt)
    {
        _logger.LogInformation("WallpaperChanged received. CorrelationId={CorrelationId}", evt.CorrelationId);
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var imageBytes = await _httpClient.GetByteArrayAsync(evt.ImageUrl, cts.Token);

            var extension = Path.GetExtension(new Uri(evt.ImageUrl).AbsolutePath);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            var localPath = Path.Combine(
                FileSystem.CacheDirectory,
                $"wallpaper-{evt.CorrelationId}{extension}");

            await File.WriteAllBytesAsync(localPath, imageBytes, cts.Token);

            var applied = await _wallpaperService.TryApplyWallpaperAsync(localPath, cts.Token);
            if (!applied)
            {
                _logger.LogInformation(
                    "Wallpaper downloaded but requires user action (iOS). CorrelationId={CorrelationId}",
                    evt.CorrelationId);
            }
            else
            {
                _logger.LogInformation(
                    "Wallpaper applied. CorrelationId={CorrelationId}", evt.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wallpaper sync failed. CorrelationId={CorrelationId}", evt.CorrelationId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hub.DisposeAsync();
    }
}
