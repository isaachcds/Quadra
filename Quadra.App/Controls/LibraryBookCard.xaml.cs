using System.Windows.Input;
using Quadra.App.ViewModels;

namespace Quadra.App.Controls;

public partial class LibraryBookCard : ContentView
{
    public static readonly BindableProperty ItemProperty = BindableProperty.Create(
        nameof(Item),
        typeof(LibraryBookViewData),
        typeof(LibraryBookCard));

    public static readonly BindableProperty OpenCommandProperty = BindableProperty.Create(
        nameof(OpenCommand),
        typeof(ICommand),
        typeof(LibraryBookCard));

    public static readonly BindableProperty DeleteCommandProperty = BindableProperty.Create(
        nameof(DeleteCommand),
        typeof(ICommand),
        typeof(LibraryBookCard));

    public LibraryBookCard()
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

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }
}
