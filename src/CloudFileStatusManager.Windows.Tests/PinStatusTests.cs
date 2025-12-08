using CloudFileStatusManager.Enums;
using CloudFileStatusManager.Windows;
using Xunit;

namespace CloudFileStatusManager.Windows.Tests;

/// <summary>
/// Unit tests for pin status detection logic.
/// These tests verify that file attributes are correctly interpreted to determine
/// whether a cloud file is pinned (always kept local) or unpinned (can be dehydrated).
/// </summary>
public class PinStatusTests
{
    // Mirror the file attribute constants for test readability.
    private const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
    private const uint FILE_ATTRIBUTE_PINNED = 0x00080000;
    private const uint FILE_ATTRIBUTE_UNPINNED = 0x00100000;
    private const uint FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = 0x00400000;

    #region Pinned Status Tests

    [Fact]
    public void DeterminePinStatus_PinnedAttribute_ReturnsPinned()
    {
        uint attributes = FILE_ATTRIBUTE_PINNED;

        var result = WindowsCloudFileStatusManager.DeterminePinStatusFromAttributes(attributes);

        Assert.Equal(FilePinStatus.Pinned, result);
    }

    [Fact]
    public void DeterminePinStatus_PinnedWithOtherAttributes_ReturnsPinned()
    {
        // PINNED with other attributes should still return Pinned.
        uint attributes = FILE_ATTRIBUTE_PINNED | FILE_ATTRIBUTE_OFFLINE | FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS;

        var result = WindowsCloudFileStatusManager.DeterminePinStatusFromAttributes(attributes);

        Assert.Equal(FilePinStatus.Pinned, result);
    }

    [Fact]
    public void DeterminePinStatus_PinnedAndUnpinned_ReturnsPinned()
    {
        // Edge case: both PINNED and UNPINNED (shouldn't normally happen).
        // PINNED takes precedence in the current implementation.
        uint attributes = FILE_ATTRIBUTE_PINNED | FILE_ATTRIBUTE_UNPINNED;

        var result = WindowsCloudFileStatusManager.DeterminePinStatusFromAttributes(attributes);

        Assert.Equal(FilePinStatus.Pinned, result);
    }

    #endregion

    #region Unpinned Status Tests

    [Fact]
    public void DeterminePinStatus_UnpinnedAttribute_ReturnsUnpinned()
    {
        uint attributes = FILE_ATTRIBUTE_UNPINNED;

        var result = WindowsCloudFileStatusManager.DeterminePinStatusFromAttributes(attributes);

        Assert.Equal(FilePinStatus.Unpinned, result);
    }

    [Fact]
    public void DeterminePinStatus_UnpinnedWithOtherAttributes_ReturnsUnpinned()
    {
        // UNPINNED with other (non-PINNED) attributes.
        uint attributes = FILE_ATTRIBUTE_UNPINNED | FILE_ATTRIBUTE_OFFLINE | FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS;

        var result = WindowsCloudFileStatusManager.DeterminePinStatusFromAttributes(attributes);

        Assert.Equal(FilePinStatus.Unpinned, result);
    }

    [Fact]
    public void DeterminePinStatus_NoAttributes_ReturnsUnpinned()
    {
        // Default case: no pin-related attributes = unpinned.
        uint attributes = 0;

        var result = WindowsCloudFileStatusManager.DeterminePinStatusFromAttributes(attributes);

        Assert.Equal(FilePinStatus.Unpinned, result);
    }

    [Fact]
    public void DeterminePinStatus_OnlyOtherAttributes_ReturnsUnpinned()
    {
        // Non-pin attributes should default to unpinned.
        uint attributes = FILE_ATTRIBUTE_OFFLINE | FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS;

        var result = WindowsCloudFileStatusManager.DeterminePinStatusFromAttributes(attributes);

        Assert.Equal(FilePinStatus.Unpinned, result);
    }

    #endregion

    #region Theory Tests

    [Theory]
    [InlineData(FILE_ATTRIBUTE_PINNED, FilePinStatus.Pinned)]
    [InlineData(FILE_ATTRIBUTE_UNPINNED, FilePinStatus.Unpinned)]
    [InlineData(FILE_ATTRIBUTE_PINNED | FILE_ATTRIBUTE_UNPINNED, FilePinStatus.Pinned)]
    [InlineData(0u, FilePinStatus.Unpinned)]
    [InlineData(FILE_ATTRIBUTE_OFFLINE, FilePinStatus.Unpinned)]
    [InlineData(FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS, FilePinStatus.Unpinned)]
    public void DeterminePinStatus_VariousAttributeCombinations_ReturnsExpected(
        uint attributes, FilePinStatus expected)
    {
        var result = WindowsCloudFileStatusManager.DeterminePinStatusFromAttributes(attributes);

        Assert.Equal(expected, result);
    }

    #endregion
}
