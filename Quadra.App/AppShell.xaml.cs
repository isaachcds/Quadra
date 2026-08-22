using Quadra.App.Pages;

namespace Quadra.App;

public partial class AppShell : Shell
{
    public AppShell(
        BibliotecaPage libraryPage,
        ColecoesPage collectionsPage,
        HistoricoPage historyPage,
        SettingsPage settingsPage)
    {
        InitializeComponent();

        Routing.RegisterRoute("BookDetailsPage", typeof(DetalhesObraPage));
        Routing.RegisterRoute("ReaderPage", typeof(LeitorPage));
        Routing.RegisterRoute("EpubReaderPage", typeof(LeitorEpubPage));

        var mainTabs = new TabBar { Route = "main" };
        mainTabs.Items.Add(CreateTab(
            "Biblioteca",
            "library",
            "icon_library.svg",
            "BottomNavLibrary",
            libraryPage));
        mainTabs.Items.Add(CreateTab(
            "Coleções",
            "collections",
            "icon_collections.svg",
            "BottomNavCollections",
            collectionsPage));
        mainTabs.Items.Add(CreateTab(
            "Histórico",
            "history",
            "icon_history.svg",
            "BottomNavHistory",
            historyPage));
        mainTabs.Items.Add(CreateTab(
            "Configurações",
            "settings",
            "icon_settings.svg",
            "BottomNavSettings",
            settingsPage));

        Items.Add(mainTabs);
    }

    private static Tab CreateTab(
        string title,
        string route,
        string icon,
        string automationId,
        Page page)
    {
        var tab = new Tab
        {
            Title = title,
            Route = route,
            Icon = icon,
            AutomationId = automationId
        };

        tab.Items.Add(new ShellContent
        {
            Route = $"{route}Content",
            Content = page
        });

        return tab;
    }
}
