using CloudFileStatusManager.Enums;
using CloudFileStatusManager.Windows;
using Xunit;
using static CloudFileStatusManager.Windows.ExternalFileUtils;

namespace CloudFileStatusManager.Windows.Tests;

/// <summary>
/// Unit tests for Cloud Filter API-based hydration status detection.
/// These tests verify that placeholder state flags are correctly interpreted
/// to determine hydration status.
/// </summary>
public class PlaceholderStateTests
{
    // File attribute constants for test readability.
    private const uint FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = 0x00400000;
    private const uint FILE_ATTRIBUTE_RECALL_ON_OPEN = 0x00040000;
    private const uint FILE_ATTRIBUTE_PINNED = 0x00080000;
    private const uint FILE_ATTRIBUTE_UNPINNED = 0x00100000;

    #region Non-Placeholder Tests

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_NoPlaceholderState_ReturnsHydrated()
    {
        // A file with no placeholder flags is a regular file - hydrated.
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_NO_STATES;
        uint attributes = 0;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Hydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_SyncRootOnly_ReturnsHydrated()
    {
        // A sync root without placeholder flag is considered hydrated.
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_SYNC_ROOT;
        uint attributes = 0;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Hydrated, result);
    }

    #endregion

    #region Hydrating (Partial) Tests

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_PartialPlaceholder_ReturnsHydrating()
    {
        // A placeholder with PARTIAL flag is being populated - hydrating.
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER
                    | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL;
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS | FILE_ATTRIBUTE_PINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Hydrating, result);
    }

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_PartiallyOnDisk_ReturnsHydrating()
    {
        // A placeholder with PARTIALLY_ON_DISK flag is partially hydrated - hydrating.
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER
                    | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIALLY_ON_DISK;
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Hydrating, result);
    }

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_PartialAndPartiallyOnDisk_ReturnsHydrating()
    {
        // Both partial flags set - definitely hydrating.
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER
                    | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL
                    | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIALLY_ON_DISK;
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS | FILE_ATTRIBUTE_PINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Hydrating, result);
    }

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_PartialWithInSync_ReturnsHydrating()
    {
        // Partial with IN_SYNC - still hydrating (download in progress).
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER
                    | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC
                    | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL;
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Hydrating, result);
    }

    #endregion

    #region Dehydrated Placeholder Tests

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_PlaceholderWithRecall_ReturnsDehydrated()
    {
        // A placeholder with recall attribute but not pinned - dehydrated.
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER;
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Dehydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_PlaceholderWithRecallOnOpen_ReturnsDehydrated()
    {
        // A placeholder with RECALL_ON_OPEN - dehydrated.
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER;
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_OPEN;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Dehydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_PlaceholderWithRecallAndUnpinned_ReturnsDehydrated()
    {
        // Typical dehydrated file: placeholder, recall needed, unpinned.
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER
                    | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC;
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS | FILE_ATTRIBUTE_UNPINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Dehydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_PlaceholderWithRecallAndPinned_ReturnsHydrating()
    {
        // Placeholder with recall and PINNED - user requested hydration, hydrating.
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER;
        uint attributes = FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS | FILE_ATTRIBUTE_PINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Hydrating, result);
    }

    #endregion

    #region Hydrated Placeholder Tests

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_PlaceholderWithoutRecall_ReturnsHydrated()
    {
        // A placeholder with no recall attributes - fully hydrated.
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER;
        uint attributes = 0;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Hydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_PlaceholderInSyncNoRecall_ReturnsHydrated()
    {
        // Placeholder is in sync and doesn't need recall - fully hydrated.
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER
                    | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC;
        uint attributes = FILE_ATTRIBUTE_PINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Hydrated, result);
    }

    [Fact]
    public void DetermineHydrationStatusFromPlaceholderState_PlaceholderWithUnpinnedNoRecall_ReturnsHydrated()
    {
        // Hydrated placeholder marked as unpinned (can be dehydrated later).
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER;
        uint attributes = FILE_ATTRIBUTE_UNPINNED;

        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(FileHydrationStatus.Hydrated, result);
    }

    #endregion

    #region Theory Tests

    [Theory]
    [InlineData(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_NO_STATES, 0u, FileHydrationStatus.Hydrated)]
    [InlineData(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER, FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS, FileHydrationStatus.Dehydrated)]
    [InlineData(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER, FILE_ATTRIBUTE_RECALL_ON_OPEN, FileHydrationStatus.Dehydrated)]
    [InlineData(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER, FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS | FILE_ATTRIBUTE_PINNED, FileHydrationStatus.Hydrating)]
    [InlineData(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL, 0u, FileHydrationStatus.Hydrating)]
    [InlineData(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIALLY_ON_DISK, 0u, FileHydrationStatus.Hydrating)]
    [InlineData(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER, 0u, FileHydrationStatus.Hydrated)]
    [InlineData(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER, FILE_ATTRIBUTE_PINNED, FileHydrationStatus.Hydrated)]
    public void DetermineHydrationStatusFromPlaceholderState_VariousCombinations_ReturnsExpected(
        CF_PLACEHOLDER_STATE state, uint attributes, FileHydrationStatus expected)
    {
        var result = WindowsCloudFileStatusManager.DetermineHydrationStatusFromPlaceholderState(state, attributes);

        Assert.Equal(expected, result);
    }

    #endregion

    #region IsFullyHydrated Helper Tests

    [Fact]
    public void IsFullyHydrated_NoPlaceholderState_ReturnsTrue()
    {
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_NO_STATES;

        var result = ExternalFileUtils.IsFullyHydrated(state);

        Assert.True(result);
    }

    [Fact]
    public void IsFullyHydrated_PlaceholderNoPartial_ReturnsTrue()
    {
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER
                    | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC;

        var result = ExternalFileUtils.IsFullyHydrated(state);

        Assert.True(result);
    }

    [Fact]
    public void IsFullyHydrated_PlaceholderPartial_ReturnsFalse()
    {
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER
                    | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL;

        var result = ExternalFileUtils.IsFullyHydrated(state);

        Assert.False(result);
    }

    [Fact]
    public void IsFullyHydrated_PlaceholderPartiallyOnDisk_ReturnsFalse()
    {
        var state = CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER
                    | CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIALLY_ON_DISK;

        var result = ExternalFileUtils.IsFullyHydrated(state);

        Assert.False(result);
    }

    #endregion
}
