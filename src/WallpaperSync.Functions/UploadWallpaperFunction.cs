using System.Net;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WallpaperSync.Contracts;

namespace WallpaperSync.Functions;

/// <summary>
/// HTTP-triggered function that:
///   1. Accepts a wallpaper image upload (multipart or raw bytes).
///   2. Stores the image in Azure Blob Storage.
///   3. Broadcasts a <see cref="WallpaperChangedEvent"/> via Azure SignalR Service
///      so all connected clients receive a real-time update.
/// </summary>
public sealed class UploadWallpaperFunction
{
    private readonly BlobServiceClient _blobService;
    private readonly ILogger<UploadWallpaperFunction> _logger;

    private static readonly string ContainerName =
        Environment.GetEnvironmentVariable("WallpaperBlobContainer") ?? "wallpapers";

    public UploadWallpaperFunction(BlobServiceClient blobService,
        ILogger<UploadWallpaperFunction> logger)
    {
        _blobService = blobService;
        _logger = logger;
    }

    /// <summary>
    /// Accepts POST /api/upload with the image in the request body.
    /// Returns the URL of the stored blob and broadcasts to SignalR.
    /// </summary>
    [Function(nameof(UploadWallpaperFunction))]
    [SignalROutput(HubName = "wallpaperhub", ConnectionStringSetting = "AzureSignalRConnectionString")]
    public async Task<UploadResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload")] HttpRequestData req,
        CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var extension = req.Headers.TryGetValues("Content-Type", out var contentTypeValues)
            ? ResolveExtension(contentTypeValues.FirstOrDefault())
            : ".jpg";
        var blobName = $"{correlationId}{extension}";

        _logger.LogInformation("Uploading wallpaper blob {BlobName}", blobName);

        var container = _blobService.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(
            Azure.Storage.Blobs.Models.PublicAccessType.Blob, cancellationToken: ct);

        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(req.Body, overwrite: true, cancellationToken: ct);

        var imageUrl = blob.Uri.ToString();
        _logger.LogInformation("Wallpaper stored at {Url}", imageUrl);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(new { imageUrl, correlationId }, ct);

        return new UploadResult
        {
            HttpResponse = response,
            SignalRMessage = new SignalRMessageAction("WallpaperChanged")
            {
                Arguments = [new WallpaperChangedEvent(imageUrl, correlationId)]
            }
        };
    }

    private static string ResolveExtension(string? contentType) => contentType?.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".jpg"
    };
}

/// <summary>
/// Multi-output binding envelope: HTTP response + SignalR broadcast.
/// </summary>
public sealed class UploadResult
{
    public HttpResponseData HttpResponse { get; init; } = null!;
    public SignalRMessageAction SignalRMessage { get; init; } = null!;
}
