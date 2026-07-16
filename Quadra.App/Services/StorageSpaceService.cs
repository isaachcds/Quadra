namespace Quadra.App.Services;

public sealed class StorageSpaceService : IStorageSpaceService
{
    public StorageSpaceInfo GetAvailableSpace(string destinationPath)
    {
        try
        {
            var fullPath = Path.GetFullPath(destinationPath);
            var root = Path.GetPathRoot(fullPath);

            if (string.IsNullOrWhiteSpace(root))
                return StorageSpaceInfo.Unknown;

            var available = new DriveInfo(root).AvailableFreeSpace;
            return available < 0
                ? StorageSpaceInfo.Unknown
                : StorageSpaceInfo.Known(available);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            IOException or
            UnauthorizedAccessException)
        {
            return StorageSpaceInfo.Unknown;
        }
    }
}
