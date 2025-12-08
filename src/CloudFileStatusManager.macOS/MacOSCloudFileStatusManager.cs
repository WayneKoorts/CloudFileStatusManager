using System.Runtime.InteropServices;
using CloudFileStatusManager.Enums;

namespace CloudFileStatusManager.macOS;

/// <summary>
/// macOS implementation of ICloudFileStatusManager.
/// Uses NSFileManager and NSURL APIs to detect and manage iCloud Drive file status.
/// </summary>
public class MacOSCloudFileStatusManager : ICloudFileStatusManager
{
    public bool IsOnCloudStorage(string filePath, bool verbose = false)
    {
        var result = CloudStorageDetector.DetectCloudStorage(filePath, verbose);
        if (verbose) Console.WriteLine($"IsOnCloudStorage result: {result.IsCloudFile}");
        return result.IsCloudFile;
    }

    public Task<bool> IsOnCloudStorageAsync(string filePath, bool verbose = false)
    {
        return Task.FromResult(IsOnCloudStorage(filePath, verbose));
    }

    public FileHydrationStatus GetHydrationStatus(string filePath, bool verbose = false)
    {
        if (verbose) Console.WriteLine($"Getting hydration status for {filePath}");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var nsUrl = NativeInterop.CreateNSURLFromPath(filePath);
        if (nsUrl == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create NSURL from path");
        }

        // Get ubiquitous item downloading status
        var downloadingStatus = GetUrlResourceValue(nsUrl, NativeInterop.NSURLUbiquitousItemDownloadingStatusKey);
        var isDownloading = GetUrlResourceBoolValue(nsUrl, NativeInterop.NSURLUbiquitousItemIsDownloadingKey);

        if (verbose)
        {
            var statusStr = NativeInterop.NSStringToString(downloadingStatus) ?? "(null)";
            Console.WriteLine($"Downloading status: {statusStr}");
            Console.WriteLine($"Is downloading: {isDownloading}");
            PrintDownloadingStatusConstants(verbose);
        }

        // Check if file is currently downloading
        if (isDownloading)
        {
            return FileHydrationStatus.Hydrating;
        }

        // Check downloading status
        if (downloadingStatus == IntPtr.Zero)
        {
            // Not a ubiquitous item or status unavailable - assume hydrated
            if (verbose) Console.WriteLine("No downloading status - assuming hydrated (local file)");
            return FileHydrationStatus.Hydrated;
        }

        // Compare with known status constants
        if (NativeInterop.NSObjectEquals(downloadingStatus, NativeInterop.NSURLUbiquitousItemDownloadingStatusNotDownloaded))
        {
            if (verbose) Console.WriteLine("Status: NotDownloaded -> Dehydrated");
            return FileHydrationStatus.Dehydrated;
        }

        if (NativeInterop.NSObjectEquals(downloadingStatus, NativeInterop.NSURLUbiquitousItemDownloadingStatusDownloaded) ||
            NativeInterop.NSObjectEquals(downloadingStatus, NativeInterop.NSURLUbiquitousItemDownloadingStatusCurrent))
        {
            if (verbose) Console.WriteLine("Status: Downloaded/Current -> Hydrated");
            return FileHydrationStatus.Hydrated;
        }

        // Unknown status - default to hydrated
        if (verbose) Console.WriteLine("Unknown status - defaulting to Hydrated");
        return FileHydrationStatus.Hydrated;
    }

    public FilePinStatus GetPinStatus(string filePath, bool verbose = false)
    {
        if (verbose) Console.WriteLine($"Getting pin status for {filePath}");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        // macOS uses the extended attribute "com.apple.fileprovider.pinned#PX" to indicate
        // whether a file is pinned (Keep Downloaded). A value of "1" means pinned.
        const string pinnedAttribute = "com.apple.fileprovider.pinned#PX";

        var isPinned = HasExtendedAttributeWithValue(filePath, pinnedAttribute, "1", verbose);

        if (verbose)
        {
            Console.WriteLine($"File is pinned (Keep Downloaded): {isPinned}");
        }

        return isPinned ? FilePinStatus.Pinned : FilePinStatus.Unpinned;
    }

    private static bool HasExtendedAttributeWithValue(string filePath, string attributeName, string expectedValue, bool verbose)
    {
        // Use getxattr to read the extended attribute
        var buffer = new byte[256];
        var size = NativeInterop.getxattr(filePath, attributeName, buffer, buffer.Length, 0, 0);

        if (verbose)
        {
            Console.WriteLine($"getxattr for '{attributeName}' returned size: {size}");
        }

        if (size <= 0)
        {
            return false;
        }

        var value = System.Text.Encoding.UTF8.GetString(buffer, 0, (int)size).TrimEnd('\0');

        if (verbose)
        {
            Console.WriteLine($"Extended attribute value: '{value}'");
        }

        return value == expectedValue;
    }

    public void HydrateFile(string filePath, bool verbose = false)
    {
        if (verbose) Console.WriteLine($"Hydrating file {filePath}");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var status = GetHydrationStatus(filePath, verbose);
        if (status == FileHydrationStatus.Hydrated)
        {
            Console.WriteLine("File is already hydrated.");
            return;
        }

        // Use NSFileManager to start downloading the ubiquitous item
        var fileManager = NativeInterop.GetDefaultFileManager();
        var nsUrl = NativeInterop.CreateNSURLFromPath(filePath);

        if (nsUrl == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create NSURL from path");
        }

        var startDownloadingSel = NativeInterop.sel_registerName("startDownloadingUbiquitousItemAtURL:error:");
        var errorPtr = IntPtr.Zero;

        var success = NativeInterop.objc_msgSend_bool(fileManager, startDownloadingSel, nsUrl, errorPtr);

        if (!success)
        {
            throw new InvalidOperationException("Failed to start downloading ubiquitous item");
        }

        if (verbose) Console.WriteLine("Started downloading file from iCloud");

        // Wait for the file to finish hydrating
        if (verbose) Console.WriteLine("Waiting for file to finish hydrating...");
        do
        {
            Thread.Sleep(1000);
            status = GetHydrationStatus(filePath, verbose);
        } while (status != FileHydrationStatus.Hydrated);

        if (verbose) Console.WriteLine("File hydration complete");
    }

    public void DehydrateFile(string filePath, bool verbose = false)
    {
        if (verbose) Console.WriteLine($"Dehydrating file {filePath}");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var status = GetHydrationStatus(filePath, verbose);
        if (status == FileHydrationStatus.Dehydrated)
        {
            Console.WriteLine("File is already dehydrated.");
            return;
        }

        // Remove the pinned attribute first to prevent automatic re-download
        // This matches Windows behavior where dehydrate also unpins the file
        const string pinnedAttribute = "com.apple.fileprovider.pinned#PX";
        var removeResult = NativeInterop.removexattr(filePath, pinnedAttribute, 0);
        if (verbose)
        {
            if (removeResult == 0)
                Console.WriteLine("Removed pinned attribute (Keep Downloaded disabled)");
            else
                Console.WriteLine("No pinned attribute to remove (file was not pinned)");
        }

        // Use NSFileManager to evict the ubiquitous item
        var fileManager = NativeInterop.GetDefaultFileManager();
        var nsUrl = NativeInterop.CreateNSURLFromPath(filePath);

        if (nsUrl == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create NSURL from path");
        }

        var evictSel = NativeInterop.sel_registerName("evictUbiquitousItemAtURL:error:");
        var errorPtr = IntPtr.Zero;

        var success = NativeInterop.objc_msgSend_bool(fileManager, evictSel, nsUrl, errorPtr);

        if (!success)
        {
            throw new InvalidOperationException("Failed to evict ubiquitous item");
        }

        if (verbose) Console.WriteLine("File evicted from local storage");
    }

    private static IntPtr GetUrlResourceValue(IntPtr nsUrl, IntPtr key)
    {
        if (nsUrl == IntPtr.Zero || key == IntPtr.Zero) return IntPtr.Zero;

        var getResourceValueSel = NativeInterop.sel_registerName("getResourceValue:forKey:error:");
        var valuePtr = IntPtr.Zero;
        var valuePtrHandle = GCHandle.Alloc(valuePtr, GCHandleType.Pinned);

        try
        {
            var success = NativeInterop.objc_msgSend_bool(
                nsUrl,
                getResourceValueSel,
                valuePtrHandle.AddrOfPinnedObject(),
                key,
                IntPtr.Zero);

            if (!success)
            {
                return IntPtr.Zero;
            }

            return Marshal.ReadIntPtr(valuePtrHandle.AddrOfPinnedObject());
        }
        finally
        {
            valuePtrHandle.Free();
        }
    }

    private static bool GetUrlResourceBoolValue(IntPtr nsUrl, IntPtr key)
    {
        var value = GetUrlResourceValue(nsUrl, key);
        if (value == IntPtr.Zero) return false;
        return NativeInterop.NSNumberToBool(value);
    }

    private static void PrintDownloadingStatusConstants(bool verbose)
    {
        if (!verbose) return;

        Console.WriteLine("Status constants:");
        var notDownloaded = NativeInterop.NSStringToString(NativeInterop.NSURLUbiquitousItemDownloadingStatusNotDownloaded);
        var downloaded = NativeInterop.NSStringToString(NativeInterop.NSURLUbiquitousItemDownloadingStatusDownloaded);
        var current = NativeInterop.NSStringToString(NativeInterop.NSURLUbiquitousItemDownloadingStatusCurrent);
        Console.WriteLine($"  NotDownloaded: {notDownloaded ?? "(null)"}");
        Console.WriteLine($"  Downloaded: {downloaded ?? "(null)"}");
        Console.WriteLine($"  Current: {current ?? "(null)"}");
    }
}
