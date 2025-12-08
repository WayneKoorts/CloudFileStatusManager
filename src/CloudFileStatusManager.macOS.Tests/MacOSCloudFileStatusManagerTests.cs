using CloudFileStatusManager.Enums;
using CloudFileStatusManager.macOS;
using Xunit;

namespace CloudFileStatusManager.macOS.Tests;

/// <summary>
/// Unit tests for macOS cloud file status manager.
/// Note: Tests that interact with iCloud Drive require running on actual macOS
/// with iCloud Drive configured and logged in.
/// </summary>
public class MacOSCloudFileStatusManagerTests
{
    private readonly MacOSCloudFileStatusManager _manager = new();

    #region File Not Found Tests

    [Fact]
    public void GetHydrationStatus_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _manager.GetHydrationStatus("/nonexistent/path/file.txt"));
    }

    [Fact]
    public void GetPinStatus_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _manager.GetPinStatus("/nonexistent/path/file.txt"));
    }

    [Fact]
    public void HydrateFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _manager.HydrateFile("/nonexistent/path/file.txt"));
    }

    [Fact]
    public void DehydrateFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _manager.DehydrateFile("/nonexistent/path/file.txt"));
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void MacOSCloudFileStatusManager_ImplementsInterface()
    {
        Assert.IsAssignableFrom<ICloudFileStatusManager>(_manager);
    }

    [Fact]
    public void IsOnCloudStorageAsync_ReturnsTask()
    {
        var task = _manager.IsOnCloudStorageAsync("/nonexistent/file.txt");
        Assert.IsType<Task<bool>>(task);
    }

    #endregion

    #region Hydration Status Enum Tests

    [Fact]
    public void FileHydrationStatus_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(FileHydrationStatus), FileHydrationStatus.Hydrated));
        Assert.True(Enum.IsDefined(typeof(FileHydrationStatus), FileHydrationStatus.Dehydrated));
        Assert.True(Enum.IsDefined(typeof(FileHydrationStatus), FileHydrationStatus.Hydrating));
    }

    #endregion

    #region Pin Status Enum Tests

    [Fact]
    public void FilePinStatus_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(FilePinStatus), FilePinStatus.Pinned));
        Assert.True(Enum.IsDefined(typeof(FilePinStatus), FilePinStatus.Unpinned));
    }

    #endregion
}
