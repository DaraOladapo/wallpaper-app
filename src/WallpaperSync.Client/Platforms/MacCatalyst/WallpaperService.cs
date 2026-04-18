using WallpaperSync.Contracts;

namespace WallpaperSync.Client.Platforms.MacCatalyst;

/// <summary>
/// macOS wallpaper service.
/// Invokes AppleScript via <c>osascript</c> to set the Finder desktop picture.
/// Requires the host (macOS) process to have Automation permissions for Finder.
/// </summary>
public sealed class WallpaperService : IWallpaperService
{
    public async Task<bool> TryApplyWallpaperAsync(string localImagePath, CancellationToken ct)
    {
        var script = $"tell application \"Finder\" to set desktop picture to POSIX file \"{EscapeAppleScript(localImagePath)}\"";

        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                Arguments = $"-e '{script}'",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync(ct);
        return process.ExitCode == 0;
    }

    private static string EscapeAppleScript(string path)
        => path.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
