namespace Quadra.App.Services;

public enum StorageSpaceStatus
{
    Unknown,
    Sufficient,
    Insufficient
}

public readonly record struct StorageSpaceCheck(
    StorageSpaceStatus Status,
    long? AvailableBytes,
    long EstimatedOperationBytes,
    long SafetyMarginBytes,
    long RequiredBytes);

public sealed class InsufficientStorageException : IOException
{
    public InsufficientStorageException(string message)
        : base(message)
    {
    }
}

public static class StorageSpacePolicy
{
    public const long MinimumSafetyMarginBytes = 64L * 1024 * 1024;
    public const int SafetyMarginPercentage = 10;
    public const long CoverAllowanceBytes = 16L * 1024 * 1024;

    public static long EstimateImportBytes(long sourceBytes)
    {
        return AddSaturating(Math.Max(0, sourceBytes), CoverAllowanceBytes);
    }

    public static long EstimatePdfPageBytes(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return 0;

        return Math.Min(
            checked((long)width * height * 4),
            FileProcessingLimits.MaximumPdfBitmapPixels * 4);
    }

    public static StorageSpaceCheck Check(
        StorageSpaceInfo space,
        long estimatedOperationBytes)
    {
        // Required bytes = the operation estimate plus the larger of:
        // 64 MiB, or 10% of the estimate (rounded up).
        var estimate = Math.Max(0, estimatedOperationBytes);
        var percentageMargin = AddSaturating(
            estimate / SafetyMarginPercentage,
            estimate % SafetyMarginPercentage == 0 ? 0 : 1);
        var safety = Math.Max(MinimumSafetyMarginBytes, percentageMargin);
        var required = AddSaturating(estimate, safety);

        if (!space.IsKnown)
        {
            return new StorageSpaceCheck(
                StorageSpaceStatus.Unknown,
                null,
                estimate,
                safety,
                required);
        }

        return new StorageSpaceCheck(
            space.AvailableBytes!.Value >= required
                ? StorageSpaceStatus.Sufficient
                : StorageSpaceStatus.Insufficient,
            space.AvailableBytes,
            estimate,
            safety,
            required);
    }

    public static StorageSpaceCheck EnsureAvailable(
        IStorageSpaceService service,
        string destinationPath,
        long estimatedOperationBytes,
        string insufficientMessage)
    {
        ArgumentNullException.ThrowIfNull(service);

        var check = Check(
            service.GetAvailableSpace(destinationPath),
            estimatedOperationBytes);

        System.Diagnostics.Debug.WriteLine(
            $"Storage check: path={destinationPath}; estimate={check.EstimatedOperationBytes} bytes; " +
            $"safety={check.SafetyMarginBytes} bytes; available={check.AvailableBytes?.ToString() ?? "unknown"} bytes; " +
            $"required={check.RequiredBytes} bytes; status={check.Status}.");

        if (check.Status == StorageSpaceStatus.Insufficient)
            throw new InsufficientStorageException(insufficientMessage);

        return check;
    }

    private static long AddSaturating(long left, long right)
    {
        return left > long.MaxValue - right
            ? long.MaxValue
            : left + right;
    }
}
