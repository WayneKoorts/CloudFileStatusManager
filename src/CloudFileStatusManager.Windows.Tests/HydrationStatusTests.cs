using CloudFileStatusManager.Enums;
using CloudFileStatusManager.Windows;
using Xunit;

namespace CloudFileStatusManager.Windows.Tests;

/// <summary>
/// Unit tests for hydration status detection logic.
/// These tests verify that file attributes are correctly interpreted to determine
/// whether a cloud file is hydrated, dehydrated, or currently hydrating.
/// </summary>
public class HydrationStatusTests
{
    // Mirror the file attribute constants for test readability.
    private const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200;
    private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
    private const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
    private const uint FILE_ATTRIBUTE_RECALL_ON_OPEN = 0x00040000;
    private const uint FILE_ATTRIBUTE_PINNED = 0x00080000;
    private const uint FILE_ATTRIBUTE_UNPINNED = 0x00100000;
    private const uint FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = 0x00400000;

    #region Dehydrated Status Tests

    [Fact]
    public void DetermineHydrationStatus_RecallOnDataAccess_ReturnsDehydrated()
    {
        // A file with RECALL_ON_DATA_ACCESS but not PINNED is a dehydrated placeholder.
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Dehydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatus_RecallOnOpen_ReturnsDehydrated()
    {
        // A file with RECALL_ON_OPEN but not PINNED is a dehydrated placeholder.
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_OPEN;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Dehydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatus_RecallOnDataAccessAndUnpinned_ReturnsDehydrated()
    {
        // Common case: dehydrated cloud-only file that's marked as unpinned.
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS | FILE_ATTRIBUTE_UNPINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Dehydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatus_RecallOnDataAccessWithOffline_ReturnsDehydrated()
    {
        // Dehydrated file that also has OFFLINE attribute set.
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS | FILE_ATTRIBUTE_OFFLINE;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Dehydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatus_RecallOnDataAccessWithSparseAndReparse_ReturnsDehydrated()
    {
        // Typical cloud placeholder: sparse file, reparse point, needs data recall.
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS
                          | FILE_ATTRIBUTE_SPARSE_FILE
                          | FILE_ATTRIBUTE_REPARSE_POINT;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Dehydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatus_BothRecallAttributes_ReturnsDehydrated()
    {
        // File with both recall attributes (without PINNED) is still dehydrated.
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS | FILE_ATTRIBUTE_RECALL_ON_OPEN;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Dehydrated, result);
    }

    #endregion

    #region Hydrating Status Tests

    [Fact]
    public void DetermineHydrationStatus_RecallOnDataAccessAndPinned_ReturnsHydrating()
    {
        // A file with RECALL_ON_DATA_ACCESS AND PINNED is being downloaded.
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS | FILE_ATTRIBUTE_PINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Hydrating, result);
    }

    [Fact]
    public void DetermineHydrationStatus_RecallOnOpenAndPinned_ReturnsHydrating()
    {
        // A file with RECALL_ON_OPEN AND PINNED is being downloaded.
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_OPEN | FILE_ATTRIBUTE_PINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Hydrating, result);
    }

    [Fact]
    public void DetermineHydrationStatus_BothRecallAttributesAndPinned_ReturnsHydrating()
    {
        // File with both recall attributes and PINNED is hydrating.
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS
                          | FILE_ATTRIBUTE_RECALL_ON_OPEN
                          | FILE_ATTRIBUTE_PINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Hydrating, result);
    }

    [Fact]
    public void DetermineHydrationStatus_RecallWithPinnedAndOtherAttributes_ReturnsHydrating()
    {
        // Hydrating file with additional attributes.
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS
                          | FILE_ATTRIBUTE_PINNED
                          | FILE_ATTRIBUTE_SPARSE_FILE
                          | FILE_ATTRIBUTE_REPARSE_POINT;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Hydrating, result);
    }

    #endregion

    #region Hydrated Status Tests

    [Fact]
    public void DetermineHydrationStatus_NoAttributes_ReturnsHydrated()
    {
        // A regular file with no special attributes is considered hydrated.
        uint attributes = 0;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Hydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatus_PinnedOnly_ReturnsHydrated()
    {
        // A pinned file without recall attributes is fully hydrated (always available locally).
        uint attributes = FILE_ATTRIBUTE_PINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Hydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatus_UnpinnedOnly_ReturnsHydrated()
    {
        // An unpinned file without recall attributes is currently hydrated
        // (but may be dehydrated later by the system).
        uint attributes = FILE_ATTRIBUTE_UNPINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Hydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatus_OfflineOnly_ReturnsHydrated()
    {
        // OFFLINE alone (without recall attributes) doesn't mean dehydrated.
        uint attributes = FILE_ATTRIBUTE_OFFLINE;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Hydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatus_SparseAndReparseOnly_ReturnsHydrated()
    {
        // Sparse + reparse point without recall attributes is hydrated.
        // (Could be a non-cloud sparse file or a hydrated cloud file.)
        uint attributes = FILE_ATTRIBUTE_SPARSE_FILE | FILE_ATTRIBUTE_REPARSE_POINT;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Hydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatus_PinnedAndUnpinned_ReturnsHydrated()
    {
        // Edge case: both PINNED and UNPINNED (shouldn't normally happen).
        // Without recall attributes, file is hydrated.
        uint attributes = FILE_ATTRIBUTE_PINNED | FILE_ATTRIBUTE_UNPINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Hydrated, result);
    }

    #endregion

    #region Edge Cases and Regression Tests

    [Fact]
    public void DetermineHydrationStatus_AllCloudAttributesExceptRecall_ReturnsHydrated()
    {
        // File has many cloud-related attributes but no recall = hydrated.
        uint attributes = FILE_ATTRIBUTE_PINNED
                          | FILE_ATTRIBUTE_SPARSE_FILE
                          | FILE_ATTRIBUTE_REPARSE_POINT
                          | FILE_ATTRIBUTE_OFFLINE;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Hydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatus_RecallWithUnpinnedNotPinned_ReturnsDehydrated()
    {
        // Regression test: RECALL + UNPINNED (without PINNED) should be dehydrated, not hydrating.
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS | FILE_ATTRIBUTE_UNPINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Dehydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatus_RecallWithBothPinnedAndUnpinned_ReturnsHydrating()
    {
        // Edge case: RECALL + PINNED + UNPINNED (PINNED takes precedence for hydrating).
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS
                          | FILE_ATTRIBUTE_PINNED
                          | FILE_ATTRIBUTE_UNPINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(FileHydrationStatus.Hydrating, result);
    }

    [Theory]
    [InlineData(FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS, FileHydrationStatus.Dehydrated)]
    [InlineData(FILE_ATTRIBUTE_RECALL_ON_OPEN, FileHydrationStatus.Dehydrated)]
    [InlineData(FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS | FILE_ATTRIBUTE_PINNED, FileHydrationStatus.Hydrating)]
    [InlineData(FILE_ATTRIBUTE_RECALL_ON_OPEN | FILE_ATTRIBUTE_PINNED, FileHydrationStatus.Hydrating)]
    [InlineData(FILE_ATTRIBUTE_PINNED, FileHydrationStatus.Hydrated)]
    [InlineData(FILE_ATTRIBUTE_UNPINNED, FileHydrationStatus.Hydrated)]
    [InlineData(0u, FileHydrationStatus.Hydrated)]
    public void DetermineHydrationStatus_VariousAttributeCombinations_ReturnsExpected(
        uint attributes, FileHydrationStatus expected)
    {
        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromAttributes(attributes);

        Assert.Equal(expected, result);
    }

    #endregion
}
