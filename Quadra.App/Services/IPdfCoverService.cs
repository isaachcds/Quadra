using Quadra.App.Models;

namespace Quadra.App.Services;

public interface IPdfCoverService
{
    Task<string?> GenerateCoverAsync(
        LibraryItem item,
        string destinationPath,
        CancellationToken cancellationToken = default);
}