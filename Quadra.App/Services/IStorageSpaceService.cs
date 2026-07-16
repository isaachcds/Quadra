namespace Quadra.App.Services;

public readonly record struct StorageSpaceInfo(long? AvailableBytes)
{
    public bool IsKnown => AvailableBytes.HasValue;

    public static StorageSpaceInfo Unknown => new(null);
    public static StorageSpaceInfo Known(long availableBytes) =>
        new(Math.Max(0, availableBytes));
}

public interface IStorageSpaceService
{
    StorageSpaceInfo GetAvailableSpace(string destinationPath);
}
