using CloudFileStatusManager.Enums;

namespace CloudFileStatusManager.Windows;

public class WindowsCloudFileStatusManager : ICloudFileStatusManager
{
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
            Console.WriteLine($"  Pinned? {(attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) != 0}");
            Console.WriteLine($"  Unpinned? {(attributes & ExternalFileUtils.FILE_ATTRIBUTE_UNPINNED) != 0}");
            Console.WriteLine($"  Offline? {(attributes & ExternalFileUtils.FILE_ATTRIBUTE_OFFLINE) != 0}");
            Console.WriteLine($"  Recall on data access? {(attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) != 0}");
        }
        
        // If the attributes contain recall on data access and unpinned, the file is dehydrated.
        if ((attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) != 0
            && (attributes & ExternalFileUtils.FILE_ATTRIBUTE_UNPINNED) != 0)
        {
            return FileHydrationStatus.Dehydrated;
        }
        // Check conditions for hydrated state.
        // Hydrated is either:
        // 1. Pinned and not recall on data access and not unpinned, or
        // 2. Not recall on data access and not unpinned and not pinned.
        if (
            // Pinned
            ((attributes & ExternalFileUtils.FILE_ATTRIBUTE_PINNED) != 0 
            // and not Recall on data access
            && (attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) == 0
            // and not Offline
            && (attributes & ExternalFileUtils.FILE_ATTRIBUTE_OFFLINE) == 0
            // and not Unpinned
            && (attributes & ExternalFileUtils.FILE_ATTRIBUTE_UNPINNED) == 0 )
            //--
            // Or
            //--
            ||
            // Not Recall on data access
            (attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) == 0
            // and not Unpinned
            && (attributes & ExternalFileUtils.FILE_ATTRIBUTE_UNPINNED) == 0
            // and not Offline
            && (attributes & ExternalFileUtils.FILE_ATTRIBUTE_OFFLINE) == 0
            // and not Pinned
            && (attributes & ExternalFileUtils.FILE_ATTRIBUTE_PINNED) == 0)
        {
            return FileHydrationStatus.Hydrated;
        }
        // Else if the attributes contain recall on data access and pinned, or just recall
        // on data access, the file is hydrating.
        if ((attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) != 0
              && (attributes & ExternalFileUtils.FILE_ATTRIBUTE_PINNED) != 0
            || (attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) != 0)
        {
            return FileHydrationStatus.Hydrating;
        }

        // Default to hydrated if none of the above conditions are met.
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
