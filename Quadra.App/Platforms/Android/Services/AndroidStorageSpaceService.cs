using Android.OS;
using Quadra.App.Services;

namespace Quadra.App.Platforms.Android.Services;

public sealed class AndroidStorageSpaceService : IStorageSpaceService
{
    public StorageSpaceInfo GetAvailableSpace(string destinationPath)
    {
        try
        {
            var fullPath = Path.GetFullPath(destinationPath);
            var directory = Directory.Exists(fullPath)
                ? fullPath
                : Path.GetDirectoryName(fullPath);

            if (string.IsNullOrWhiteSpace(directory))
                return StorageSpaceInfo.Unknown;

            using var statistics = new StatFs(directory);
            var availableBytes = checked(
                statistics.AvailableBlocksLong * statistics.BlockSizeLong);

            return availableBytes < 0
                ? StorageSpaceInfo.Unknown
                : StorageSpaceInfo.Known(availableBytes);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            IOException or
            UnauthorizedAccessException or
            OverflowException or
            Java.Lang.Exception)
        {
            return StorageSpaceInfo.Unknown;
        }
    }
}
