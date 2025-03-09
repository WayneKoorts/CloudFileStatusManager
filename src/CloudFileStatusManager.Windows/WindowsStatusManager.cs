using CloudFileStatusManager.Enums;

namespace CloudFileStatusManager.Windows;

public class WindowsCloudFileStatusManager : ICloudFileStatusManager
{
    public FileHydrationStatus GetHydrationStatus(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        var attributes = ExternalFileUtils.GetAttributesViaHandleEx(filePath);
        
        // If the attributes contain recall on data access and unpinned, the file is dehydrated.
        if ((attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) != 0
            && (attributes & ExternalFileUtils.FILE_ATTRIBUTE_UNPINNED) != 0)
        {
            return FileHydrationStatus.Dehydrated;
        }
        // If the attributes contain pinned, or neither recall on data access nor unpinned,
        // the file is hydrated.
        if ((attributes & ExternalFileUtils.FILE_ATTRIBUTE_PINNED) != 0
            || (attributes & ExternalFileUtils.FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS) == 0
                && (attributes & ExternalFileUtils.FILE_ATTRIBUTE_UNPINNED) == 0)
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

    public FilePinStatus GetPinStatus(string filePath)
    {
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

    public void HydrateFile(string filePath)
    {
        throw new NotImplementedException();
    }

    public void DehydrateFile(string filePath)
    {
        throw new NotImplementedException();
    }
}
