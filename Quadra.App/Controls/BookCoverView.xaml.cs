namespace Quadra.App.Controls;

public partial class BookCoverView : ContentView
{
    public static readonly BindableProperty CoverPathProperty = BindableProperty.Create(
        nameof(CoverPath),
        typeof(string),
        typeof(BookCoverView));

    public static readonly BindableProperty HasCoverProperty = BindableProperty.Create(
        nameof(HasCover),
        typeof(bool),
        typeof(BookCoverView));

    public static readonly BindableProperty FormatProperty = BindableProperty.Create(
        nameof(Format),
        typeof(string),
        typeof(BookCoverView),
        string.Empty);

    public static readonly BindableProperty CoverDescriptionProperty = BindableProperty.Create(
        nameof(CoverDescription),
        typeof(string),
        typeof(BookCoverView),
        string.Empty);

    public BookCoverView()
    {
        InitializeComponent();
    }

    public string? CoverPath
    {
        get => (string?)GetValue(CoverPathProperty);
        set => SetValue(CoverPathProperty, value);
    }

    public bool HasCover
    {
        get => (bool)GetValue(HasCoverProperty);
        set => SetValue(HasCoverProperty, value);
    }

    public string Format
    {
        get => (string)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    public string CoverDescription
    {
        get => (string)GetValue(CoverDescriptionProperty);
        set => SetValue(CoverDescriptionProperty, value);
    }
}
