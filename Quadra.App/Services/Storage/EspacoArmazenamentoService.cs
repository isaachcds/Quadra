namespace Quadra.App.Services.Storage;

public sealed class EspacoArmazenamentoService : IEspacoArmazenamentoService
{
    public InformacoesEspacoArmazenamento ObterEspacoDisponivel(string destinationPath)
    {
        try
        {
            var fullPath = Path.GetFullPath(destinationPath);
            var root = Path.GetPathRoot(fullPath);

            if (string.IsNullOrWhiteSpace(root))
                return InformacoesEspacoArmazenamento.Desconhecido;

            var available = new DriveInfo(root).AvailableFreeSpace;
            return available < 0
                ? InformacoesEspacoArmazenamento.Desconhecido
                : InformacoesEspacoArmazenamento.ComBytesDisponiveis(available);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            IOException or
            UnauthorizedAccessException)
        {
            return InformacoesEspacoArmazenamento.Desconhecido;
        }
    }
}
