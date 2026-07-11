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

        Items.Add(new ShellContent
        {
            Title = "Biblioteca",
            Route = nameof(LibraryPage),
            Content = libraryPage
        });
    }
}