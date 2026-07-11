using Quadra.App.Pages;

namespace Quadra.App;

public partial class AppShell : Shell
{
    public AppShell(LibraryPage libraryPage)
    {
        InitializeComponent();

        Routing.RegisterRoute(
            nameof(BookDetailsPage),
            typeof(BookDetailsPage));

        Routing.RegisterRoute(
            nameof(ReaderPage),
            typeof(ReaderPage));

        Items.Add(new ShellContent
        {
            Title = "Biblioteca",
            Route = nameof(LibraryPage),
            Content = libraryPage
        });
    }
}