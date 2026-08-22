using Quadra.App.Services.Storage;

namespace Quadra.App.Policies;

public enum StatusEspacoArmazenamento
{
    Desconhecido,
    Suficiente,
    Insuficiente
}

public readonly record struct VerificacaoEspacoArmazenamento(
    StatusEspacoArmazenamento Status,
    long? BytesDisponiveis,
    long BytesOperacaoEstimados,
    long BytesMargemSeguranca,
    long BytesNecessarios);

public sealed class EspacoArmazenamentoInsuficienteException : IOException
{
    public EspacoArmazenamentoInsuficienteException(string message)
        : base(message)
    {
    }
}

public static class PoliticaEspacoArmazenamento
{
    public const long MargemSegurancaMinimaBytes = 64L * 1024 * 1024;
    public const int PercentualMargemSeguranca = 10;
    public const long ReservaCapaBytes = 16L * 1024 * 1024;

    public static long EstimarBytesImportacao(long sourceBytes)
    {
        return SomarSaturado(Math.Max(0, sourceBytes), ReservaCapaBytes);
    }

    public static long EstimarBytesPaginaPdf(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return 0;

        return Math.Min(
            checked((long)width * height * 4),
            FileProcessingLimits.MaximumPdfBitmapPixels * 4);
    }

    public static VerificacaoEspacoArmazenamento Verificar(
        InformacoesEspacoArmazenamento space,
        long estimatedOperationBytes)
    {
        // Required bytes = the operation estimate plus the larger of:
        // 64 MiB, or 10% of the estimate (rounded up).
        var estimate = Math.Max(0, estimatedOperationBytes);
        var percentageMargin = SomarSaturado(
            estimate / PercentualMargemSeguranca,
            estimate % PercentualMargemSeguranca == 0 ? 0 : 1);
        var safety = Math.Max(MargemSegurancaMinimaBytes, percentageMargin);
        var required = SomarSaturado(estimate, safety);

        if (!space.EspacoConhecido)
        {
            return new VerificacaoEspacoArmazenamento(
                StatusEspacoArmazenamento.Desconhecido,
                null,
                estimate,
                safety,
                required);
        }

        return new VerificacaoEspacoArmazenamento(
            space.BytesDisponiveis!.Value >= required
                ? StatusEspacoArmazenamento.Suficiente
                : StatusEspacoArmazenamento.Insuficiente,
            space.BytesDisponiveis,
            estimate,
            safety,
            required);
    }

    public static VerificacaoEspacoArmazenamento GarantirDisponivel(
        IEspacoArmazenamentoService service,
        string destinationPath,
        long estimatedOperationBytes,
        string insufficientMessage)
    {
        ArgumentNullException.ThrowIfNull(service);

        var check = Verificar(
            service.ObterEspacoDisponivel(destinationPath),
            estimatedOperationBytes);

        System.Diagnostics.Debug.WriteLine(
            $"Storage check: path={destinationPath}; estimate={check.BytesOperacaoEstimados} bytes; " +
            $"safety={check.BytesMargemSeguranca} bytes; available={check.BytesDisponiveis?.ToString() ?? "unknown"} bytes; " +
            $"required={check.BytesNecessarios} bytes; status={check.Status}.");

        if (check.Status == StatusEspacoArmazenamento.Insuficiente)
            throw new EspacoArmazenamentoInsuficienteException(insufficientMessage);

        return check;
    }

    private static long SomarSaturado(long left, long right)
    {
        return left > long.MaxValue - right
            ? long.MaxValue
            : left + right;
    }
}
