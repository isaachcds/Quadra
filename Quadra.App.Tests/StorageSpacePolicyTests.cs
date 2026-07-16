using Quadra.App.Services;

namespace Quadra.App.Tests;

public sealed class StorageSpacePolicyTests
{
    [Fact]
    public void KnownSpace_IsSufficientWhenItMeetsRequirement()
    {
        var check = StorageSpacePolicy.Check(
            StorageSpaceInfo.Known(200L * 1024 * 1024),
            100L * 1024 * 1024);

        Assert.Equal(StorageSpaceStatus.Sufficient, check.Status);
    }

    [Fact]
    public void KnownSpace_IsInsufficientWhenBelowRequirement()
    {
        var check = StorageSpacePolicy.Check(
            StorageSpaceInfo.Known(100L * 1024 * 1024),
            100L * 1024 * 1024);

        Assert.Equal(StorageSpaceStatus.Insufficient, check.Status);
    }

    [Fact]
    public void UnknownSpace_IsNotTreatedAsZero()
    {
        var check = StorageSpacePolicy.Check(
            StorageSpaceInfo.Unknown,
            100L * 1024 * 1024);

        Assert.Equal(StorageSpaceStatus.Unknown, check.Status);
        Assert.Null(check.AvailableBytes);
    }

    [Fact]
    public void Calculation_UsesLongWithoutOverflowForMultiGigabyteValue()
    {
        const long fiveGigabytes = 5L * 1024 * 1024 * 1024;

        var check = StorageSpacePolicy.Check(
            StorageSpaceInfo.Known(8L * 1024 * 1024 * 1024),
            fiveGigabytes);

        Assert.True(check.RequiredBytes > fiveGigabytes);
        Assert.Equal(StorageSpaceStatus.Sufficient, check.Status);
    }

    [Fact]
    public void SmallImport_DoesNotReserveGlobalMaximum()
    {
        const long oneMegabyte = 1024L * 1024;

        var estimate = StorageSpacePolicy.EstimateImportBytes(oneMegabyte);

        Assert.Equal(oneMegabyte + StorageSpacePolicy.CoverAllowanceBytes, estimate);
        Assert.True(estimate < FileProcessingLimits.MaximumImportBytes);
        Assert.True(estimate < FileProcessingLimits.MaximumExpandedBytes);
    }

    [Fact]
    public void SafetyMargin_UsesMinimumForSmallFiles()
    {
        var check = StorageSpacePolicy.Check(
            StorageSpaceInfo.Known(long.MaxValue),
            1024);

        Assert.Equal(StorageSpacePolicy.MinimumSafetyMarginBytes, check.SafetyMarginBytes);
    }

    [Fact]
    public void SafetyMargin_UsesPercentageForLargeFiles()
    {
        const long oneGigabyte = 1024L * 1024 * 1024;

        var check = StorageSpacePolicy.Check(
            StorageSpaceInfo.Known(long.MaxValue),
            oneGigabyte);

        Assert.Equal(107_374_183, check.SafetyMarginBytes);
    }

    [Fact]
    public void Comparison_IsPerformedInBytesAtExactBoundary()
    {
        var initial = StorageSpacePolicy.Check(StorageSpaceInfo.Unknown, 25_000_000);

        var enough = StorageSpacePolicy.Check(
            StorageSpaceInfo.Known(initial.RequiredBytes),
            initial.EstimatedOperationBytes);
        var insufficient = StorageSpacePolicy.Check(
            StorageSpaceInfo.Known(initial.RequiredBytes - 1),
            initial.EstimatedOperationBytes);

        Assert.Equal(StorageSpaceStatus.Sufficient, enough.Status);
        Assert.Equal(StorageSpaceStatus.Insufficient, insufficient.Status);
    }

    [Fact]
    public void PdfImport_EstimatesOnlySourceAndCoverNotWholeFutureCache()
    {
        const long pdfBytes = 12L * 1024 * 1024;

        var estimate = StorageSpacePolicy.EstimateImportBytes(pdfBytes);

        Assert.Equal(pdfBytes + StorageSpacePolicy.CoverAllowanceBytes, estimate);
        Assert.NotEqual(FileProcessingLimits.MaximumExpandedBytes, estimate);
    }

    [Fact]
    public void EnsureAvailable_AllowsUnknownMeasurement()
    {
        var service = new FakeStorageSpaceService(StorageSpaceInfo.Unknown);

        var check = StorageSpacePolicy.EnsureAvailable(
            service,
            "destination",
            1024,
            "insuficiente");

        Assert.Equal(StorageSpaceStatus.Unknown, check.Status);
    }

    [Fact]
    public void EnsureAvailable_BlocksConfirmedInsufficientSpace()
    {
        const string message = "Não há espaço disponível suficiente para importar este arquivo.";
        var service = new FakeStorageSpaceService(StorageSpaceInfo.Known(1));

        var exception = Assert.Throws<InsufficientStorageException>(() =>
            StorageSpacePolicy.EnsureAvailable(
                service,
                "destination",
                1024,
                message));

        Assert.Equal(message, exception.Message);
    }

    private sealed class FakeStorageSpaceService(StorageSpaceInfo result)
        : IStorageSpaceService
    {
        public StorageSpaceInfo GetAvailableSpace(string destinationPath) => result;
    }
}
