namespace CloudFileStatusManager.Enums;

public enum FileHydrationStatus
{
    /// <summary>
    /// <para>
    /// A hydrated file exists on the cloud drive and has been downloaded to
    /// the local machine.
    /// </para>
    /// <para>
    /// Represented in File Explorer by either an unfilled green check icon
    /// (if the file is pinned) or a filled green check icon (if the file is
    /// unpinned).
    /// </para>
    /// <seealso cref="FilePinStatus" />
    /// </summary>
    Hydrated,

    /// <summary>
    /// <para>
    /// A dehydrated file exists on the cloud drive but has not been downloaded
    /// to the local machine. It is represented by a placeholder file on the local
    /// machine.
    /// </para>
    /// <para>
    /// Represented in File Explorer by an unfilled cloud icon with a blue outline.
    /// </para>
    /// </summary>
    /// 
    Dehydrated,

    /// <summary>
    /// A file is in the process of being hydrated. It is being downloaded from
    /// from the cloud provider to the local machine and will transition to the
    /// dehydrated state once the download is complete.
    /// </summary>
    Hydrating,
}