using Quadra.App.Policies;
using Quadra.App.Services.Storage;

namespace Quadra.App.Tests;

public sealed class PoliticaEspacoArmazenamentoTests
{
    [Fact]
    public void KnownSpace_IsSufficientWhenItMeetsRequirement()
    {
        var check = PoliticaEspacoArmazenamento.Verificar(
            InformacoesEspacoArmazenamento.ComBytesDisponiveis(200L * 1024 * 1024),
            100L * 1024 * 1024);

        Assert.Equal(StatusEspacoArmazenamento.Suficiente, check.Status);
    }

    [Fact]
    public void KnownSpace_IsInsufficientWhenBelowRequirement()
    {
        var check = PoliticaEspacoArmazenamento.Verificar(
            InformacoesEspacoArmazenamento.ComBytesDisponiveis(100L * 1024 * 1024),
            100L * 1024 * 1024);

        Assert.Equal(StatusEspacoArmazenamento.Insuficiente, check.Status);
    }

    [Fact]
    public void UnknownSpace_IsNotTreatedAsZero()
    {
        var check = PoliticaEspacoArmazenamento.Verificar(
            InformacoesEspacoArmazenamento.Desconhecido,
            100L * 1024 * 1024);

        Assert.Equal(StatusEspacoArmazenamento.Desconhecido, check.Status);
        Assert.Null(check.BytesDisponiveis);
    }

    [Fact]
    public void Calculation_UsesLongWithoutOverflowForMultiGigabyteValue()
    {
        const long fiveGigabytes = 5L * 1024 * 1024 * 1024;

        var check = PoliticaEspacoArmazenamento.Verificar(
            InformacoesEspacoArmazenamento.ComBytesDisponiveis(8L * 1024 * 1024 * 1024),
            fiveGigabytes);

        Assert.True(check.BytesNecessarios > fiveGigabytes);
        Assert.Equal(StatusEspacoArmazenamento.Suficiente, check.Status);
    }

    [Fact]
    public void SmallImport_DoesNotReserveGlobalMaximum()
    {
        const long oneMegabyte = 1024L * 1024;

        var estimate = PoliticaEspacoArmazenamento.EstimarBytesImportacao(oneMegabyte);

        Assert.Equal(oneMegabyte + PoliticaEspacoArmazenamento.ReservaCapaBytes, estimate);
        Assert.True(estimate < FileProcessingLimits.MaximumImportBytes);
        Assert.True(estimate < FileProcessingLimits.MaximumExpandedBytes);
    }

    [Fact]
    public void SafetyMargin_UsesMinimumForSmallFiles()
    {
        var check = PoliticaEspacoArmazenamento.Verificar(
            InformacoesEspacoArmazenamento.ComBytesDisponiveis(long.MaxValue),
            1024);

        Assert.Equal(PoliticaEspacoArmazenamento.MargemSegurancaMinimaBytes, check.BytesMargemSeguranca);
    }

    [Fact]
    public void SafetyMargin_UsesPercentageForLargeFiles()
    {
        const long oneGigabyte = 1024L * 1024 * 1024;

        var check = PoliticaEspacoArmazenamento.Verificar(
            InformacoesEspacoArmazenamento.ComBytesDisponiveis(long.MaxValue),
            oneGigabyte);

        Assert.Equal(107_374_183, check.BytesMargemSeguranca);
    }

    [Fact]
    public void Comparison_IsPerformedInBytesAtExactBoundary()
    {
        var initial = PoliticaEspacoArmazenamento.Verificar(InformacoesEspacoArmazenamento.Desconhecido, 25_000_000);

        var enough = PoliticaEspacoArmazenamento.Verificar(
            InformacoesEspacoArmazenamento.ComBytesDisponiveis(initial.BytesNecessarios),
            initial.BytesOperacaoEstimados);
        var insufficient = PoliticaEspacoArmazenamento.Verificar(
            InformacoesEspacoArmazenamento.ComBytesDisponiveis(initial.BytesNecessarios - 1),
            initial.BytesOperacaoEstimados);

        Assert.Equal(StatusEspacoArmazenamento.Suficiente, enough.Status);
        Assert.Equal(StatusEspacoArmazenamento.Insuficiente, insufficient.Status);
    }

    [Fact]
    public void ImportacaoPdf_EstimaSomenteOrigemECapaSemCacheFuturoCompleto()
    {
        const long pdfBytes = 12L * 1024 * 1024;

        var estimate = PoliticaEspacoArmazenamento.EstimarBytesImportacao(pdfBytes);

        Assert.Equal(pdfBytes + PoliticaEspacoArmazenamento.ReservaCapaBytes, estimate);
        Assert.NotEqual(FileProcessingLimits.MaximumExpandedBytes, estimate);
    }

    [Fact]
    public void EnsureAvailable_AllowsUnknownMeasurement()
    {
        var service = new EspacoArmazenamentoFalsoService(InformacoesEspacoArmazenamento.Desconhecido);

        var check = PoliticaEspacoArmazenamento.GarantirDisponivel(
            service,
            "destination",
            1024,
            "insuficiente");

        Assert.Equal(StatusEspacoArmazenamento.Desconhecido, check.Status);
    }

    [Fact]
    public void EnsureAvailable_BlocksConfirmedInsufficientSpace()
    {
        const string message = "Não há espaço disponível suficiente para importar este arquivo.";
        var service = new EspacoArmazenamentoFalsoService(InformacoesEspacoArmazenamento.ComBytesDisponiveis(1));

        var exception = Assert.Throws<EspacoArmazenamentoInsuficienteException>(() =>
            PoliticaEspacoArmazenamento.GarantirDisponivel(
                service,
                "destination",
                1024,
                message));

        Assert.Equal(message, exception.Message);
    }

    private sealed class EspacoArmazenamentoFalsoService(InformacoesEspacoArmazenamento result)
        : IEspacoArmazenamentoService
    {
        public InformacoesEspacoArmazenamento ObterEspacoDisponivel(string destinationPath) => result;
    }
}
