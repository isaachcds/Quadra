using System.Windows.Input;
using Quadra.App.ViewModels;

namespace Quadra.App.Controls;

public partial class CartaoContinuarLeitura : ContentView
{
    public static readonly BindableProperty ObraProperty = BindableProperty.Create(
        nameof(Obra),
        typeof(DadosObraBiblioteca),
        typeof(CartaoContinuarLeitura));

    public static readonly BindableProperty AbrirCommandProperty = BindableProperty.Create(
        nameof(AbrirCommand),
        typeof(ICommand),
        typeof(CartaoContinuarLeitura));

    public CartaoContinuarLeitura()
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
}
