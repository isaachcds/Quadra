using Quadra.App.Models;

namespace Quadra.App.Services.Covers;

public interface ICapaPdfService
{
    Task<string?> GerarCapaAsync(
        ObraBiblioteca item,
        string destinationPath,
        CancellationToken cancellationToken = default);
}
