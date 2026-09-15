using WallpaperSync.Contracts;

namespace WallpaperSync.Tests;

public class WallpaperChangedEventTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        const string url = "https://example.blob.core.windows.net/wallpapers/abc.jpg";
        const string id = "abc123";

        var evt = new WallpaperChangedEvent(url, id);

        Assert.Equal(url, evt.ImageUrl);
        Assert.Equal(id, evt.CorrelationId);
    }

    [Fact]
    public void Records_SupportValueEquality()
    {
        var a = new WallpaperChangedEvent("https://example.com/img.jpg", "id1");
        var b = new WallpaperChangedEvent("https://example.com/img.jpg", "id1");
        var c = new WallpaperChangedEvent("https://other.com/img.jpg", "id2");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void Record_Deconstruct_Works()
    {
        var evt = new WallpaperChangedEvent("https://example.com/img.jpg", "xyz");
        var (imageUrl, correlationId) = evt;

        Assert.Equal("https://example.com/img.jpg", imageUrl);
        Assert.Equal("xyz", correlationId);
    }
}
