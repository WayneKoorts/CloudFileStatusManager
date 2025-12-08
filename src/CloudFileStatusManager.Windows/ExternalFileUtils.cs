using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace CloudFileStatusManager.Windows;

public static class ExternalFileUtils
{
    // Windows API constants for cloud file attributes.
    // See: https://learn.microsoft.com/en-us/windows/win32/fileio/file-attribute-constants
    internal const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200;
    internal const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
    internal const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
    internal const uint FILE_ATTRIBUTE_RECALL_ON_OPEN = 0x00040000;
    internal const uint FILE_ATTRIBUTE_PINNED = 0x00080000;
    internal const uint FILE_ATTRIBUTE_UNPINNED = 0x00100000;
    internal const uint FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = 0x00400000;
    private const int FILE_READ_ATTRIBUTES = 0x0080;
    private const int FILE_WRITE_ATTRIBUTES = 0x0100;
    private const int FILE_SHARE_READ = 0x00000001;
    private const int FILE_SHARE_WRITE = 0x00000002;
    private const int OPEN_EXISTING = 3;

    // Cloud Filter API placeholder state flags.
    // See: https://learn.microsoft.com/en-us/windows/win32/api/cfapi/ne-cfapi-cf_placeholder_state
    [Flags]
    internal enum CF_PLACEHOLDER_STATE : uint
    {
        CF_PLACEHOLDER_STATE_NO_STATES = 0x00000000,
        CF_PLACEHOLDER_STATE_PLACEHOLDER = 0x00000001,
        CF_PLACEHOLDER_STATE_SYNC_ROOT = 0x00000002,
        CF_PLACEHOLDER_STATE_ESSENTIAL_PROP_PRESENT = 0x00000004,
        CF_PLACEHOLDER_STATE_IN_SYNC = 0x00000008,
        CF_PLACEHOLDER_STATE_PARTIAL = 0x00000010,
        CF_PLACEHOLDER_STATE_PARTIALLY_ON_DISK = 0x00000020,
        CF_PLACEHOLDER_STATE_INVALID = 0xFFFFFFFF
    }

    // Cloud Filter reparse tag for cloud files.
    private const uint IO_REPARSE_TAG_CLOUD = 0x9000001A;
    private const uint IO_REPARSE_TAG_CLOUD_MASK = 0x0000F000;

    #region Kernel32 P/Invoke
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        int dwDesiredAccess,
        int dwShareMode,
        IntPtr lpSecurityAttributes,
        int dwCreationDisposition,
        int dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandleEx(
        IntPtr hFile,
        FILE_INFO_BY_HANDLE_CLASS FileInformationClass,
        out FILE_BASIC_INFO lpFileInformation,
        uint dwBufferSize
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFileInformationByHandle(
        IntPtr hFile,
        FILE_INFO_BY_HANDLE_CLASS FileInformationClass,
        ref FILE_BASIC_INFO lpFileInformation,
        uint dwBufferSize
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private enum FILE_INFO_BY_HANDLE_CLASS
    {
        FileBasicInfo = 0,
        FileAttributeTagInfo = 9
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILE_BASIC_INFO
    {
        public long CreationTime;
        public long LastAccessTime;
        public long LastWriteTime;
        public long ChangeTime;
        public uint FileAttributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILE_ATTRIBUTE_TAG_INFO
    {
        public uint FileAttributes;
        public uint ReparseTag;
    }

    // Overload for FILE_ATTRIBUTE_TAG_INFO
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandleEx(
        IntPtr hFile,
        FILE_INFO_BY_HANDLE_CLASS FileInformationClass,
        out FILE_ATTRIBUTE_TAG_INFO lpFileInformation,
        uint dwBufferSize
    );

    #endregion

    #region Cloud Filter API P/Invoke

    /// <summary>
    /// Gets the placeholder state from file attributes and reparse tag.
    /// This is the most efficient way to get placeholder state.
    /// See: https://learn.microsoft.com/en-us/windows/win32/api/cfapi/nf-cfapi-cfgetplaceholderstatefromattributetag
    /// </summary>
    [DllImport("cldapi.dll", SetLastError = false)]
    private static extern CF_PLACEHOLDER_STATE CfGetPlaceholderStateFromAttributeTag(
        uint FileAttributes,
        uint ReparseTag
    );

    #endregion

    #region File Attribute Methods

    internal static uint GetAttributesViaHandleEx(string filePath)
    {
        var hFile = CreateFile(
            filePath,
            FILE_READ_ATTRIBUTES,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            0,
            IntPtr.Zero);

        if (hFile.ToInt64() == -1) throw new IOException("Failed to open file.");

        FILE_BASIC_INFO info;
        try
        {
            if (!GetFileInformationByHandleEx(
                    hFile,
                    FILE_INFO_BY_HANDLE_CLASS.FileBasicInfo,
                    out info,
                    (uint)Marshal.SizeOf<FILE_BASIC_INFO>()))
            {
                throw new IOException("GetFileInformationByHandleEx call failed.");
            }
        }
        finally
        {
            CloseHandle(hFile);
        }

        return info.FileAttributes;
    }

    /// <summary>
    /// Gets file attributes and reparse tag information.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>Tuple of (FileAttributes, ReparseTag).</returns>
    internal static (uint FileAttributes, uint ReparseTag) GetAttributesAndReparseTag(string filePath)
    {
        var hFile = CreateFile(
            filePath,
            FILE_READ_ATTRIBUTES,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            0,
            IntPtr.Zero);

        if (hFile.ToInt64() == -1) throw new IOException("Failed to open file.");

        try
        {
            if (!GetFileInformationByHandleEx(
                    hFile,
                    FILE_INFO_BY_HANDLE_CLASS.FileAttributeTagInfo,
                    out FILE_ATTRIBUTE_TAG_INFO info,
                    (uint)Marshal.SizeOf<FILE_ATTRIBUTE_TAG_INFO>()))
            {
                throw new IOException("GetFileInformationByHandleEx (FileAttributeTagInfo) call failed.");
            }

            return (info.FileAttributes, info.ReparseTag);
        }
        finally
        {
            CloseHandle(hFile);
        }
    }

    #endregion

    #region Cloud Filter API Methods

    /// <summary>
    /// Gets the Cloud Filter placeholder state for a file.
    /// This provides authoritative information about whether a file is a cloud placeholder
    /// and its sync/hydration state.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>The placeholder state, or null if the Cloud Filter API is not available.</returns>
    internal static CF_PLACEHOLDER_STATE? GetPlaceholderState(string filePath)
    {
        try
        {
            var (attributes, reparseTag) = GetAttributesAndReparseTag(filePath);
            var state = CfGetPlaceholderStateFromAttributeTag(attributes, reparseTag);

            // CF_PLACEHOLDER_STATE_INVALID indicates the API couldn't determine the state
            // (e.g., not a cloud file, or API not available)
            if (state == CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_INVALID)
            {
                return null;
            }

            return state;
        }
        catch
        {
            // Cloud Filter API not available (e.g., older Windows version)
            return null;
        }
    }

    /// <summary>
    /// Determines if a file is a cloud placeholder using the Cloud Filter API.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>True if the file is a cloud placeholder, false otherwise.</returns>
    internal static bool IsCloudPlaceholder(string filePath)
    {
        var state = GetPlaceholderState(filePath);
        if (state == null) return false;

        return state.Value.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER);
    }

    /// <summary>
    /// Determines if a cloud placeholder file has its data fully available on disk.
    /// </summary>
    /// <param name="state">The placeholder state.</param>
    /// <returns>True if data is fully on disk (hydrated), false if dehydrated or partial.</returns>
    internal static bool IsFullyHydrated(CF_PLACEHOLDER_STATE state)
    {
        // A file is fully hydrated if:
        // 1. It's NOT a placeholder at all (regular file), OR
        // 2. It's a placeholder that is IN_SYNC and NOT PARTIAL and NOT PARTIALLY_ON_DISK
        //    (IN_SYNC with PARTIALLY_ON_DISK would be a partial hydration)

        bool isPlaceholder = state.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER);

        if (!isPlaceholder)
        {
            // Not a cloud placeholder - it's a regular file, considered "hydrated"
            return true;
        }

        // For placeholders:
        // - IN_SYNC means the placeholder metadata is in sync
        // - PARTIALLY_ON_DISK means some (but not all) data is on disk
        // - PARTIAL means the file is being populated
        bool isPartial = state.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIAL);
        bool isPartiallyOnDisk = state.HasFlag(CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PARTIALLY_ON_DISK);

        // If neither partial flag is set, the file is either fully hydrated or fully dehydrated.
        // We need to check file attributes to distinguish these cases.
        // A fully hydrated placeholder typically won't have RECALL_ON_DATA_ACCESS set.
        return !isPartial && !isPartiallyOnDisk;
    }

    #endregion

    /// <summary>
    /// <para>
    /// Sets the specified attributes on the file at the specified path.
    /// </para>
    /// <para>
    /// Supported attributes:
    /// </para>
    /// <seealso cref="FILE_ATTRIBUTE_PINNED" /><br/>
    /// <seealso cref="FILE_ATTRIBUTE_UNPINNED" /><br/>
    /// <seealso cref="FILE_ATTRIBUTE_OFFLINE" /><br/>
    /// <seealso cref="FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS" />
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="setAttributes">List of attributes to set.</param>
    /// <param name="unsetAttributes">List of attributes to unset.</param>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="IOException"></exception>
    internal static void SetAttributes(
        string filePath,
        uint[]? setAttributes = null,
        uint[]? unsetAttributes = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        if (setAttributes is { Length: 0 } && unsetAttributes is { Length: 0 })
        {
            return;
        }

        var hFile = CreateFile(
            filePath,
            FILE_WRITE_ATTRIBUTES,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            0,
            IntPtr.Zero);

        if (hFile.ToInt64() == -1) throw new IOException("Failed to open file.");

        try
        {
            if (!GetFileInformationByHandleEx(
                    hFile,
                    FILE_INFO_BY_HANDLE_CLASS.FileBasicInfo,
                    out var info,
                    (uint)Marshal.SizeOf<FILE_BASIC_INFO>()))
            {
                throw new IOException("GetFileInformationByHandleEx call failed.");
            }

            if (setAttributes is { Length: > 0 })
            {
                foreach (var attribute in setAttributes)
                {
                    info.FileAttributes |= attribute;
                }
            }

            if (unsetAttributes is { Length: > 0 })
            {
                foreach (var attribute in unsetAttributes)
                {
                    info.FileAttributes &= ~attribute;
                }
            }

            if (!SetFileInformationByHandle(
                    hFile,
                    FILE_INFO_BY_HANDLE_CLASS.FileBasicInfo,
                    ref info,
                    (uint)Marshal.SizeOf<FILE_BASIC_INFO>()))
            {
                throw new IOException("SetFileInformationByHandle call failed.");
            }
        }
        finally
        {
            CloseHandle(hFile);
        }
    }
}
