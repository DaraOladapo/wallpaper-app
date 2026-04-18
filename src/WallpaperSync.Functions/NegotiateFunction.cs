using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace WallpaperSync.Functions;

/// <summary>
/// Negotiate endpoint required by Azure SignalR Service SDK.
/// Clients call GET /api/negotiate before opening the hub connection.
/// </summary>
public static class NegotiateFunction
{
    [Function(nameof(NegotiateFunction))]
    public static SignalRConnectionInfo Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "negotiate")] HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "wallpaperhub",
            ConnectionStringSetting = "AzureSignalRConnectionString")] SignalRConnectionInfo connectionInfo)
    {
        return connectionInfo;
    }
}
