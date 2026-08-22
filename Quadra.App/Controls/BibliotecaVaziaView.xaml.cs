using System.Windows.Input;

namespace Quadra.App.Controls;

public partial class BibliotecaVaziaView : ContentView
{
    public static readonly BindableProperty ImportarCommandProperty = BindableProperty.Create(
        nameof(ImportarCommand),
        typeof(ICommand),
        typeof(BibliotecaVaziaView));

    public static readonly BindableProperty PodeImportarProperty = BindableProperty.Create(
        nameof(PodeImportar),
        typeof(bool),
        typeof(BibliotecaVaziaView),
        true);

    public BibliotecaVaziaView()
    {
        InitializeComponent();
    }

    public ICommand? ImportarCommand
    {
        get => (ICommand?)GetValue(ImportarCommandProperty);
        set => SetValue(ImportarCommandProperty, value);
    }

    public bool PodeImportar
    {
        get => (bool)GetValue(PodeImportarProperty);
        set => SetValue(PodeImportarProperty, value);
    }
}
