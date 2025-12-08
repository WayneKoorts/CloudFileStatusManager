using System.Diagnostics.CodeAnalysis;

namespace CloudFileStatusManager.macOS;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum CloudStorageProvider
{
    None,
    ICloud,
    Unknown
}

/// <summary>
/// Detects whether a file is on cloud storage (iCloud Drive) on macOS.
/// </summary>
public static class CloudStorageDetector
{
    /// <summary>
    /// Detects if a file is on cloud storage and identifies the provider.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="verbose">Enable verbose output.</param>
    /// <returns>Tuple indicating if it's a cloud file and the provider.</returns>
    public static (bool IsCloudFile, CloudStorageProvider Provider) DetectCloudStorage(
        string filePath, bool verbose = false)
    {
        if (!File.Exists(filePath))
        {
            return (false, CloudStorageProvider.None);
        }

        if (verbose) Console.WriteLine($"Checking if {filePath} is a cloud file");

        // Try using NSURL ubiquitous item key first
        try
        {
            if (IsUbiquitousItem(filePath, verbose))
            {
                var provider = DetectProviderByPath(filePath, verbose);
                return (true, provider);
            }
        }
        catch (Exception e)
        {
            if (verbose) Console.WriteLine($"Error checking ubiquitous item: {e.Message}");
        }

        // Fall back to path-based detection
        var pathProvider = DetectProviderByPath(filePath, verbose);
        if (pathProvider != CloudStorageProvider.None && pathProvider != CloudStorageProvider.Unknown)
        {
            return (true, pathProvider);
        }

        return (false, CloudStorageProvider.None);
    }

    /// <summary>
    /// Checks if a file is a ubiquitous (iCloud) item using NSURL resource values.
    /// </summary>
    private static bool IsUbiquitousItem(string filePath, bool verbose)
    {
        var nsUrl = NativeInterop.CreateNSURLFromPath(filePath);
        if (nsUrl == IntPtr.Zero)
        {
            if (verbose) Console.WriteLine("Failed to create NSURL");
            return false;
        }

        var getResourceValueSel = NativeInterop.sel_registerName("getResourceValue:forKey:error:");
        var valuePtr = IntPtr.Zero;
        var valuePtrHandle = System.Runtime.InteropServices.GCHandle.Alloc(valuePtr, System.Runtime.InteropServices.GCHandleType.Pinned);

        try
        {
            var success = NativeInterop.objc_msgSend_bool(
                nsUrl,
                getResourceValueSel,
                valuePtrHandle.AddrOfPinnedObject(),
                NativeInterop.NSURLIsUbiquitousItemKey,
                IntPtr.Zero);

            if (!success)
            {
                if (verbose) Console.WriteLine("Failed to get NSURLIsUbiquitousItemKey");
                return false;
            }

            valuePtr = System.Runtime.InteropServices.Marshal.ReadIntPtr(valuePtrHandle.AddrOfPinnedObject());
            if (valuePtr == IntPtr.Zero)
            {
                return false;
            }

            var isUbiquitous = NativeInterop.NSNumberToBool(valuePtr);
            if (verbose) Console.WriteLine($"Is ubiquitous item: {isUbiquitous}");
            return isUbiquitous;
        }
        finally
        {
            valuePtrHandle.Free();
        }
    }

    /// <summary>
    /// Detects cloud provider based on file path.
    /// </summary>
    private static CloudStorageProvider DetectProviderByPath(string filePath, bool verbose)
    {
        var path = Path.GetFullPath(filePath).ToLowerInvariant();

        // iCloud Drive paths on macOS
        // Typically: ~/Library/Mobile Documents/com~apple~CloudDocs/
        // Or: /Users/<user>/Library/Mobile Documents/
        if (path.Contains("mobile documents") ||
            path.Contains("com~apple~clouddocs") ||
            path.Contains("icloud"))
        {
            if (verbose) Console.WriteLine("Detected iCloud Drive by path");
            return CloudStorageProvider.ICloud;
        }

        return CloudStorageProvider.Unknown;
    }
}
