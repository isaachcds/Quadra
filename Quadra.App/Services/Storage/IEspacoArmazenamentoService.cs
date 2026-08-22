namespace Quadra.App.Services.Storage;

public readonly record struct InformacoesEspacoArmazenamento(long? BytesDisponiveis)
{
    public bool EspacoConhecido => BytesDisponiveis.HasValue;

    public static InformacoesEspacoArmazenamento Desconhecido => new(null);
    public static InformacoesEspacoArmazenamento ComBytesDisponiveis(long availableBytes) =>
        new(Math.Max(0, availableBytes));
}

public interface IEspacoArmazenamentoService
{
    InformacoesEspacoArmazenamento ObterEspacoDisponivel(string destinationPath);
}
