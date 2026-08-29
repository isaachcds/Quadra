namespace Quadra.App.Presentation;

public enum TemaAplicativo
{
    Sistema,
    Claro,
    Escuro
}

public static class PreferenciasAplicativo
{
    public const string ChaveTemaAplicativo = "application_theme";
    public const string ChaveOrdenacaoBiblioteca = "library_sort_option";
    public const string ChaveNavegacaoPorToque = "ReaderTapNavigationEnabled";

    public static TemaAplicativo ObterTema(IPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        var valor = preferences.Get(ChaveTemaAplicativo, (int)TemaAplicativo.Sistema);
        return Enum.IsDefined(typeof(TemaAplicativo), valor)
            ? (TemaAplicativo)valor
            : TemaAplicativo.Sistema;
    }

    public static void AplicarTema(Application application, TemaAplicativo tema)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.UserAppTheme = tema switch
        {
            TemaAplicativo.Claro => AppTheme.Light,
            TemaAplicativo.Escuro => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}
