using System.Globalization;

namespace Quadra.App.Presentation;

public enum TemaLeituraEpub
{
    Claro,
    Escuro,
    Sepia
}

public enum FonteLeituraEpub
{
    Sistema,
    SansSerif,
    Serif
}

public enum AlinhamentoLeituraEpub
{
    Esquerda,
    Justificado
}

public sealed record PreferenciasLeituraEpub(
    TemaLeituraEpub Tema,
    FonteLeituraEpub Fonte,
    double TamanhoTexto,
    double EspacamentoLinhas,
    double Margem,
    AlinhamentoLeituraEpub Alinhamento);

public static class AparenciaLeituraEpub
{
    public const double TamanhoTextoPadrao = 18;
    public const double EspacamentoLinhasPadrao = 1.7;
    public const double MargemPadrao = 20;
    public const double TamanhoTextoMinimo = 14;
    public const double TamanhoTextoMaximo = 28;
    public const double EspacamentoLinhasMinimo = 1.2;
    public const double EspacamentoLinhasMaximo = 2.4;
    public const double MargemMinima = 12;
    public const double MargemMaxima = 48;

    private const string ChaveTema = "epub_reading_theme";
    private const string ChaveFonte = "epub_reading_font";
    private const string ChaveTamanhoTexto = "epub_reading_text_size";
    private const string ChaveEspacamentoLinhas = "epub_reading_line_spacing";
    private const string ChaveMargem = "epub_reading_margin";
    private const string ChaveAlinhamento = "epub_reading_alignment";

    public static PreferenciasLeituraEpub Carregar(IPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        return new PreferenciasLeituraEpub(
            LerEnum(preferences, ChaveTema, TemaLeituraEpub.Claro),
            LerEnum(preferences, ChaveFonte, FonteLeituraEpub.Sistema),
            Limitar(preferences.Get(ChaveTamanhoTexto, TamanhoTextoPadrao), TamanhoTextoMinimo, TamanhoTextoMaximo, TamanhoTextoPadrao),
            Limitar(preferences.Get(ChaveEspacamentoLinhas, EspacamentoLinhasPadrao), EspacamentoLinhasMinimo, EspacamentoLinhasMaximo, EspacamentoLinhasPadrao),
            Limitar(preferences.Get(ChaveMargem, MargemPadrao), MargemMinima, MargemMaxima, MargemPadrao),
            LerEnum(preferences, ChaveAlinhamento, AlinhamentoLeituraEpub.Justificado));
    }

    public static void Salvar(IPreferences preferences, PreferenciasLeituraEpub preferencias)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(preferencias);

        preferences.Set(ChaveTema, (int)preferencias.Tema);
        preferences.Set(ChaveFonte, (int)preferencias.Fonte);
        preferences.Set(ChaveTamanhoTexto, preferencias.TamanhoTexto);
        preferences.Set(ChaveEspacamentoLinhas, preferencias.EspacamentoLinhas);
        preferences.Set(ChaveMargem, preferencias.Margem);
        preferences.Set(ChaveAlinhamento, (int)preferencias.Alinhamento);
    }

    public static string GerarCss(
        PreferenciasLeituraEpub preferencias,
        Color corFundo,
        Color corTexto)
    {
        ArgumentNullException.ThrowIfNull(preferencias);
        ArgumentNullException.ThrowIfNull(corFundo);
        ArgumentNullException.ThrowIfNull(corTexto);

        var tamanhoTexto = FormatarNumero(Limitar(
            preferencias.TamanhoTexto,
            TamanhoTextoMinimo,
            TamanhoTextoMaximo,
            TamanhoTextoPadrao));
        var espacamentoLinhas = FormatarNumero(Limitar(
            preferencias.EspacamentoLinhas,
            EspacamentoLinhasMinimo,
            EspacamentoLinhasMaximo,
            EspacamentoLinhasPadrao));
        var margem = FormatarNumero(Limitar(
            preferencias.Margem,
            MargemMinima,
            MargemMaxima,
            MargemPadrao));

        var familiaFonte = preferencias.Fonte switch
        {
            FonteLeituraEpub.SansSerif => "Arial, Helvetica, sans-serif",
            FonteLeituraEpub.Serif => "Georgia, 'Times New Roman', serif",
            _ => "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif"
        };

        var alinhamento = preferencias.Alinhamento == AlinhamentoLeituraEpub.Justificado
            ? "justify"
            : "left";

        return $$"""
            <style id="quadra-epub-reading-style">
                :root {
                    color-scheme: {{(preferencias.Tema == TemaLeituraEpub.Escuro ? "dark" : "light")}};
                    background-color: {{corFundo.ToHex()}} !important;
                    color: {{corTexto.ToHex()}} !important;
                }

                html,
                body {
                    background-color: {{corFundo.ToHex()}} !important;
                    color: {{corTexto.ToHex()}} !important;
                    min-height: 100%;
                    height: auto !important;
                    overflow-y: auto !important;
                    overflow-x: hidden;
                }

                body {
                    box-sizing: border-box;
                    max-width: 760px;
                    margin: 0 auto !important;
                    padding: 24px {{margem}}px 56px !important;
                    font-family: {{familiaFonte}} !important;
                    font-size: {{tamanhoTexto}}px !important;
                    line-height: {{espacamentoLinhas}} !important;
                    text-align: {{alinhamento}} !important;
                    overflow-wrap: break-word;
                    word-wrap: break-word;
                }

                p { margin-top: 0; margin-bottom: 1em; }
                h1, h2, h3, h4, h5, h6 { color: {{corTexto.ToHex()}} !important; line-height: 1.3 !important; }
                img, svg, video { display: block; max-width: 100% !important; height: auto !important; margin-left: auto; margin-right: auto; }
                table { display: block; max-width: 100%; overflow-x: auto; border-collapse: collapse; }
                pre, code { white-space: pre-wrap; overflow-wrap: break-word; }
                a { color: inherit !important; text-decoration: underline; }
            </style>
            """;
    }

    private static TEnum LerEnum<TEnum>(
        IPreferences preferences,
        string chave,
        TEnum valorPadrao)
        where TEnum : struct, Enum
    {
        var valor = preferences.Get(chave, Convert.ToInt32(valorPadrao));
        return Enum.IsDefined(typeof(TEnum), valor)
            ? (TEnum)Enum.ToObject(typeof(TEnum), valor)
            : valorPadrao;
    }

    private static double Limitar(double valor, double minimo, double maximo, double padrao)
    {
        return double.IsFinite(valor)
            ? Math.Clamp(valor, minimo, maximo)
            : padrao;
    }

    private static string FormatarNumero(double valor) =>
        valor.ToString("0.##", CultureInfo.InvariantCulture);
}
