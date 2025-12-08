using CloudFileStatusManager.Enums;

namespace CloudFileStatusManager.Windows;

public class WindowsCloudFileStatusManager : ICloudFileStatusManager
{
    public bool IsOnCloudStorage(string filePath, bool verbose = false)
    {
        return IsOnCloudStorageAsync(filePath, verbose).Result;
    }

    public async Task<bool> IsOnCloudStorageAsync(string filePath, bool verbose = false)
    {
        var result = await CloudStorageDetector.DetectCloudStorageAsync(filePath, verbose);
        if (verbose) Console.WriteLine($"IsOnCloudStorageAsync result: {result.IsCloudFile}");
        return result.IsCloudFile;
    }

    public FileHydrationStatus GetHydrationStatus(string filePath, bool verbose = false)
    {
        if (verbose) Console.WriteLine($"Getting hydration status for {filePath}");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var attributes = ExternalFileUtils.GetAttributesViaHandleEx(filePath);

        if (verbose)
        {
            Console.WriteLine("Attributes:");
            Console.WriteLine($"  Pinned? {(attributes & ExternalFileUtils.FILE_ATTRIBUTE_PINNED) != 0}");
            Console.WriteLine($"  Unpinned? {(attributes & ExternalFileUtils.FILE_ATTRIBUTE_UNPINNED) != 0}");
            Console.WriteLine($"  Offline? {(attributes & ExternalFileUtils.FILE_ATTRIBUTE_OFFLINE) != 0}");
            Console.WriteLine($"  Recall on data access? {(attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) != 0}");
            Console.WriteLine($"  Recall on open? {(attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_OPEN) != 0}");
            Console.WriteLine($"  Sparse file? {(attributes & ExternalFileUtils.FILE_ATTRIBUTE_SPARSE_FILE) != 0}");
            Console.WriteLine($"  Reparse point? {(attributes & ExternalFileUtils.FILE_ATTRIBUTE_REPARSE_POINT) != 0}");
        }

        return DetermineHydrationStatusFromAttributes(attributes);
    }

    /// <summary>
    /// Determines the hydration status based on file attributes.
    /// This method is internal to allow unit testing of the attribute logic.
    /// </summary>
    /// <param name="attributes">The file attributes from Windows API.</param>
    /// <returns>The determined hydration status.</returns>
    internal static FileHydrationStatus DetermineHydrationStatusFromAttributes(uint attributes)
    {
        // Check if file needs to recall data (i.e., content is not locally available).
        // RECALL_ON_DATA_ACCESS: Data will be fetched when file content is read.
        // RECALL_ON_OPEN: Data will be fetched when file is opened.
        bool needsRecall = (attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) != 0
                           || (attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_OPEN) != 0;

        bool isPinned = (attributes & ExternalFileUtils.FILE_ATTRIBUTE_PINNED) != 0;

        if (needsRecall)
        {
            // File content is not locally available.
            // If PINNED is set, the cloud provider is actively downloading the file.
            // Otherwise, it's a dehydrated placeholder waiting to be accessed.
            return isPinned ? FileHydrationStatus.Hydrating : FileHydrationStatus.Dehydrated;
        }

        // File content is locally available (no recall needed).
        return FileHydrationStatus.Hydrated;
    }

    public FilePinStatus GetPinStatus(string filePath, bool verbose = false)
    {
        if (verbose) Console.WriteLine($"Getting pin status for {filePath}");
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var attributes = ExternalFileUtils.GetAttributesViaHandleEx(filePath);
        return DeterminePinStatusFromAttributes(attributes);
    }

    /// <summary>
    /// Determines the pin status based on file attributes.
    /// This method is internal to allow unit testing of the attribute logic.
    /// </summary>
    /// <param name="attributes">The file attributes from Windows API.</param>
    /// <returns>The determined pin status.</returns>
    internal static FilePinStatus DeterminePinStatusFromAttributes(uint attributes)
    {
        if ((attributes & ExternalFileUtils.FILE_ATTRIBUTE_PINNED) != 0)
        {
            return FilePinStatus.Pinned;
        }
        if ((attributes & ExternalFileUtils.FILE_ATTRIBUTE_UNPINNED) != 0)
        {
            return FilePinStatus.Unpinned;
        }

        return FilePinStatus.Unpinned;
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

        // Hydrate the file by setting then unsetting the pin attribute.
        if (verbose) Console.WriteLine("Setting Pinned attribute and unsetting Unpinned attribute");
        ExternalFileUtils.SetAttributes(
            filePath,
            // Set the pinned attribute.
            [ExternalFileUtils.FILE_ATTRIBUTE_PINNED],
            // Unset the unpinned attribute.
            [ExternalFileUtils.FILE_ATTRIBUTE_UNPINNED]);

        // Wait for the file to finish hydrating.
        if (verbose) Console.WriteLine("Waiting for file to finish hydrating...");
        do
        {
            Thread.Sleep(1000);
            status = GetHydrationStatus(filePath, verbose);
        } while (status != FileHydrationStatus.Hydrated);

        // Setting the file to pinned then unpinned is a hacky way to hydrate the file,
        // but dang it, it works better than any other method I've tried.
        ExternalFileUtils.SetAttributes(
            filePath,
            unsetAttributes: [ExternalFileUtils.FILE_ATTRIBUTE_PINNED]);

        if (verbose) Console.WriteLine("Unsetting pinned attribute");
    }

    public void DehydrateFile(string filePath, bool verbose = false)
    {
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

        // Dehydrate the file by setting the Unpinned attribute.
        ExternalFileUtils.SetAttributes(
            filePath,
            setAttributes: [ExternalFileUtils.FILE_ATTRIBUTE_UNPINNED]);
    }
}
