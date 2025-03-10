using System.Diagnostics.CodeAnalysis;
using Windows.Storage;

namespace CloudFileStatusManager.Windows;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum CloudStorageProvider
{
    None,
    OneDrive,
    ICloud,
    Unknown
}

public static class CloudStorageDetector
{
    public static async Task<(bool IsCloudFile, CloudStorageProvider Provider)>
        DetectCloudStorageAsync(string filePath, bool verbose = false)
    {
        if (!File.Exists(filePath))
        {
            return (false, CloudStorageProvider.None);
        }

        try
        {
            if (verbose) Console.WriteLine($"Checking if {filePath} is a cloud file");
            // Normalize the path to Windows format (replace forward slashes with backslashes)
            // (this will prevent issues with running from, e.g. Git Bash). If we don't do
            // this then the StorageFile.GetFileFromPathAsync call will throw an exception.
            var normalisedPath = Path.GetFullPath(filePath).Replace('/', '\\');
            if (verbose) Console.WriteLine($"Normalized path: {normalisedPath}");
            var file = await StorageFile.GetFileFromPathAsync(normalisedPath);

            // Check if it's a cloud file.
            var propertiesToRetrieve = new List<string> { "System.FilePlaceholderStatus" };
            IDictionary<string, object> props = await file
                .Properties
                .RetrievePropertiesAsync(propertiesToRetrieve);

            // Check if this file has cloud properties
            if (verbose)
            {
                Console.WriteLine($"{props.Count} propert{(props.Count > 1 ? "ies" : "y")}:");
                foreach (var prop in props)
                {
                    Console.WriteLine($"  {prop.Key}: {prop.Value}");
                }
            }
            if (props.ContainsKey("System.FilePlaceholderStatus"))
            {
                // It's a cloud file, now try to determine the provider.
                var provider = DetectProviderByPath(filePath, verbose);
                return (true, provider);
            }
        }
        catch (Exception e)
        {
            // Fall back to attribute-based detection if the Windows Storage API fails.
            if (verbose) Console.WriteLine($"Error: {e.Message}");
            return (IsCloudFileByAttributes(filePath), DetectProviderByPath(filePath, verbose));
        }

        return (false, CloudStorageProvider.None);
    }

    // Fallback method using file attributes.
    private static bool IsCloudFileByAttributes(string filePath)
    {
        var attributes = ExternalFileUtils.GetAttributesViaHandleEx(filePath);
        
        return (attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) != 0 ||
               (attributes & ExternalFileUtils.FILE_ATTRIBUTE_PINNED) != 0 ||
               (attributes & ExternalFileUtils.FILE_ATTRIBUTE_UNPINNED) != 0;
    }

    // Fallback method using path analysis.
    private static CloudStorageProvider DetectProviderByPath(
        string filePath,
        bool verbose = false)
    {
        var path = Path.GetFullPath(filePath).ToLowerInvariant();
        
        var provider = CloudStorageProvider.Unknown;
        if (path.Contains("onedrive") || path.Contains("sharepoint"))
        {
            provider = CloudStorageProvider.OneDrive;
        }
            
        if (path.Contains("icloud") || path.Contains("apple\\clouddocs"))
        {
            provider = CloudStorageProvider.ICloud;
        }
        
        if (verbose) Console.WriteLine($"Detected provider: {provider}");

        return provider;
    }
}
