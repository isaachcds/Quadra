using System.Windows.Input;
using Quadra.App.ViewModels;

namespace Quadra.App.Controls;

public partial class ContinueReadingCard : ContentView
{
    public static readonly BindableProperty ItemProperty = BindableProperty.Create(
        nameof(Item),
        typeof(LibraryBookViewData),
        typeof(ContinueReadingCard));

    public static readonly BindableProperty OpenCommandProperty = BindableProperty.Create(
        nameof(OpenCommand),
        typeof(ICommand),
        typeof(ContinueReadingCard));

    public ContinueReadingCard()
    {
        InitializeComponent();
    }

    public LibraryBookViewData? Item
    {
        get => (LibraryBookViewData?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public ICommand? OpenCommand
    {
        get => (ICommand?)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }
}
