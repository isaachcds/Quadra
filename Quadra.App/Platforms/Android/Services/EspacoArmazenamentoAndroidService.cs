using Android.OS;
using Quadra.App.Services.Storage;

namespace Quadra.App.Platforms.Android.Services;

public sealed class EspacoArmazenamentoAndroidService : IEspacoArmazenamentoService
{
    public InformacoesEspacoArmazenamento ObterEspacoDisponivel(string destinationPath)
    {
        try
        {
            var fullPath = Path.GetFullPath(destinationPath);
            var directory = Directory.Exists(fullPath)
                ? fullPath
                : Path.GetDirectoryName(fullPath);

            if (string.IsNullOrWhiteSpace(directory))
                return InformacoesEspacoArmazenamento.Desconhecido;

            using var statistics = new StatFs(directory);
            var availableBytes = checked(
                statistics.AvailableBlocksLong * statistics.BlockSizeLong);

            return availableBytes < 0
                ? InformacoesEspacoArmazenamento.Desconhecido
                : InformacoesEspacoArmazenamento.ComBytesDisponiveis(availableBytes);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            IOException or
            UnauthorizedAccessException or
            OverflowException or
            Java.Lang.Exception)
        {
            return InformacoesEspacoArmazenamento.Desconhecido;
        }
    }
}
