using System.Windows.Input;
using Quadra.App.ViewModels;

namespace Quadra.App.Controls;

public partial class CartaoObraBiblioteca : ContentView
{
    public static readonly BindableProperty ObraProperty = BindableProperty.Create(
        nameof(Obra),
        typeof(DadosObraBiblioteca),
        typeof(CartaoObraBiblioteca));

    public static readonly BindableProperty AbrirCommandProperty = BindableProperty.Create(
        nameof(AbrirCommand),
        typeof(ICommand),
        typeof(CartaoObraBiblioteca));

    public static readonly BindableProperty ExcluirCommandProperty = BindableProperty.Create(
        nameof(ExcluirCommand),
        typeof(ICommand),
        typeof(CartaoObraBiblioteca));

    public CartaoObraBiblioteca()
    {
        InitializeComponent();
    }

    public DadosObraBiblioteca? Obra
    {
        get => (DadosObraBiblioteca?)GetValue(ObraProperty);
        set => SetValue(ObraProperty, value);
    }

    public ICommand? AbrirCommand
    {
        get => (ICommand?)GetValue(AbrirCommandProperty);
        set => SetValue(AbrirCommandProperty, value);
    }

    public ICommand? ExcluirCommand
    {
        get => (ICommand?)GetValue(ExcluirCommandProperty);
        set => SetValue(ExcluirCommandProperty, value);
    }
}
