using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace CloudFileStatusManager.Windows;

public static class ExternalFileUtils
{
    // Windows API constants for cloud file attributes.
    internal const int FILE_ATTRIBUTE_PINNED = 0x00080000;
    internal const int FILE_ATTRIBUTE_UNPINNED = 0x00100000;
    internal const int FILE_ATTRIBUTE_OFFLINE = 0x00001000;
    internal const int FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = 0x00400000;
    private const int FILE_READ_ATTRIBUTES = 0x0080;
    private const int FILE_WRITE_ATTRIBUTES = 0x0100;
    private const int FILE_SHARE_READ = 0x00000001;
    private const int FILE_SHARE_WRITE = 0x00000002;
    private const int OPEN_EXISTING = 3;

    // Replaced GetFileAttributes with the necessary imports and helper for GetFileInformationByHandleEx:
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
        FileBasicInfo = 0
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
