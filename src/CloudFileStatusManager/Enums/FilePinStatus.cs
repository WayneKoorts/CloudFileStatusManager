namespace CloudFileStatusManager.Enums;

public enum FilePinStatus
{
    /// <summary>
    /// <para>
    /// A pinned file is a file that exists on the cloud and has been downloaded.
    /// It is also never deleted from the local machine to free up space after a
    /// period of not being accessed.
    /// </para>
    /// <para>
    /// Represented in File Explorer by a filled green check icon.
    /// </para>
    /// </summary>
    Pinned,

    /// <summary>
    /// <para>
    /// An unpinned file is a file that exists on the cloud and has been
    /// downloaded, but may be deleted from the local machine to free up space.
    /// </para>
    /// <para>
    /// Represented in File Explorer by an unfilled green check icon.
    /// </para>
    /// </summary>
    Unpinned,
}