using System.Windows.Input;

namespace Quadra.App.Controls;

public partial class EmptyLibraryView : ContentView
{
    public static readonly BindableProperty ImportCommandProperty = BindableProperty.Create(
        nameof(ImportCommand),
        typeof(ICommand),
        typeof(EmptyLibraryView));

    public static readonly BindableProperty IsImportEnabledProperty = BindableProperty.Create(
        nameof(IsImportEnabled),
        typeof(bool),
        typeof(EmptyLibraryView),
        true);

    public EmptyLibraryView()
    {
        InitializeComponent();
    }

    public ICommand? ImportCommand
    {
        get => (ICommand?)GetValue(ImportCommandProperty);
        set => SetValue(ImportCommandProperty, value);
    }

    public bool IsImportEnabled
    {
        get => (bool)GetValue(IsImportEnabledProperty);
        set => SetValue(IsImportEnabledProperty, value);
    }
}
