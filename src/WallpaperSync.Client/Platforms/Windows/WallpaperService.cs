using System.Runtime.InteropServices;
using WallpaperSync.Contracts;

namespace WallpaperSync.Client.Platforms.Windows;

/// <summary>
/// Windows wallpaper service using Win32 SystemParametersInfo (P/Invoke).
/// </summary>
public sealed class WallpaperService : IWallpaperService
{
    // SPI_SETDESKWALLPAPER = 0x0014, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE = 0x03
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

    private const uint SpiSetDeskWallpaper = 0x0014;
    private const uint SpifUpdateAndSend = 0x0003;

    public Task<bool> TryApplyWallpaperAsync(string localImagePath, CancellationToken ct)
    {
        var result = SystemParametersInfo(SpiSetDeskWallpaper, 0, localImagePath, SpifUpdateAndSend);
        return Task.FromResult(result);
    }
}
