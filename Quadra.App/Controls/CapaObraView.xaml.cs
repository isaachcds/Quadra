namespace Quadra.App.Controls;

public partial class CapaObraView : ContentView
{
    public static readonly BindableProperty CaminhoCapaProperty = BindableProperty.Create(
        nameof(CaminhoCapa),
        typeof(string),
        typeof(CapaObraView));

    public static readonly BindableProperty PossuiCapaProperty = BindableProperty.Create(
        nameof(PossuiCapa),
        typeof(bool),
        typeof(CapaObraView));

    public static readonly BindableProperty FormatoProperty = BindableProperty.Create(
        nameof(Formato),
        typeof(string),
        typeof(CapaObraView),
        string.Empty);

    public static readonly BindableProperty DescricaoCapaProperty = BindableProperty.Create(
        nameof(DescricaoCapa),
        typeof(string),
        typeof(CapaObraView),
        string.Empty);

    public CapaObraView()
    {
        InitializeComponent();
    }

    public string? CaminhoCapa
    {
        get => (string?)GetValue(CaminhoCapaProperty);
        set => SetValue(CaminhoCapaProperty, value);
    }

    public bool PossuiCapa
    {
        get => (bool)GetValue(PossuiCapaProperty);
        set => SetValue(PossuiCapaProperty, value);
    }

    public string Formato
    {
        get => (string)GetValue(FormatoProperty);
        set => SetValue(FormatoProperty, value);
    }

    public string DescricaoCapa
    {
        get => (string)GetValue(DescricaoCapaProperty);
        set => SetValue(DescricaoCapaProperty, value);
    }
}
