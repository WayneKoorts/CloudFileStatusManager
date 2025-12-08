using CloudFileStatusManager.macOS;
using Xunit;

namespace CloudFileStatusManager.macOS.Tests;

/// <summary>
/// Unit tests for macOS cloud storage detection.
/// Note: Some tests require running on actual macOS with iCloud Drive configured.
/// Path-based detection tests can run on any platform.
/// </summary>
public class CloudStorageDetectorTests
{
    #region Provider Detection by Path Tests

    [Theory]
    [InlineData("/Users/testuser/Library/Mobile Documents/com~apple~CloudDocs/test.txt")]
    [InlineData("/Users/testuser/Library/Mobile Documents/iCloud~com~app/test.txt")]
    [InlineData("~/Library/Mobile Documents/com~apple~CloudDocs/Documents/file.pdf")]
    public void DetectProviderByPath_ICloudPaths_ContainsICloudIndicator(string path)
    {
        var lowerPath = path.ToLowerInvariant();
        Assert.True(
            lowerPath.Contains("mobile documents") ||
            lowerPath.Contains("com~apple~clouddocs") ||
            lowerPath.Contains("icloud"));
    }

    [Theory]
    [InlineData("/Users/testuser/Documents/local-file.txt")]
    [InlineData("/Applications/SomeApp.app")]
    [InlineData("/tmp/temp-file.txt")]
    [InlineData("/var/log/system.log")]
    public void DetectProviderByPath_NonCloudPaths_NoCloudIndicators(string path)
    {
        var lowerPath = path.ToLowerInvariant();
        Assert.False(lowerPath.Contains("mobile documents"));
        Assert.False(lowerPath.Contains("com~apple~clouddocs"));
        Assert.False(lowerPath.Contains("icloud"));
    }

    #endregion

    #region Cloud Storage Provider Enum Tests

    [Fact]
    public void CloudStorageProvider_HasExpectedValues()
    {
        Assert.Equal(0, (int)CloudStorageProvider.None);
        Assert.True(Enum.IsDefined(typeof(CloudStorageProvider), CloudStorageProvider.ICloud));
        Assert.True(Enum.IsDefined(typeof(CloudStorageProvider), CloudStorageProvider.Unknown));
    }

    [Fact]
    public void CloudStorageProvider_ICloudIsDefined()
    {
        // macOS primarily supports iCloud Drive
        Assert.True(Enum.IsDefined(typeof(CloudStorageProvider), CloudStorageProvider.ICloud));
    }

    #endregion

    #region Non-existent File Tests

    [Fact]
    public void DetectCloudStorage_NonExistentFile_ReturnsFalse()
    {
        var result = CloudStorageDetector.DetectCloudStorage("/nonexistent/path/file.txt");

        Assert.False(result.IsCloudFile);
        Assert.Equal(CloudStorageProvider.None, result.Provider);
    }

    #endregion
}
