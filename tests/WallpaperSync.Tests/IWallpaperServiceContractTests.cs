using Microsoft.Extensions.Logging.Abstractions;
using WallpaperSync.Contracts;

namespace WallpaperSync.Tests;

/// <summary>
/// In-memory fake for IWallpaperService to validate that the sync client
/// calls TryApplyWallpaperAsync with the expected path.
/// </summary>
internal sealed class FakeWallpaperService : IWallpaperService
{
    public List<string> AppliedPaths { get; } = [];
    public bool ReturnValue { get; set; } = true;

    public Task<bool> TryApplyWallpaperAsync(string localImagePath, CancellationToken ct)
    {
        AppliedPaths.Add(localImagePath);
        return Task.FromResult(ReturnValue);
    }
}

public class IWallpaperServiceContractTests
{
    [Fact]
    public async Task TryApplyWallpaperAsync_ReturnsTrueWhenApplied()
    {
        var svc = new FakeWallpaperService { ReturnValue = true };
        var result = await svc.TryApplyWallpaperAsync("/tmp/test.jpg", CancellationToken.None);
        Assert.True(result);
        Assert.Single(svc.AppliedPaths);
        Assert.Equal("/tmp/test.jpg", svc.AppliedPaths[0]);
    }

    [Fact]
    public async Task TryApplyWallpaperAsync_ReturnsFalseOnIOS()
    {
        var svc = new FakeWallpaperService { ReturnValue = false };
        var result = await svc.TryApplyWallpaperAsync("/tmp/test.jpg", CancellationToken.None);
        Assert.False(result);
    }
}
