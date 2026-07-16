namespace Quadra.App.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        VersionLabel.Text = $"Versão {AppInfo.Current.VersionString}";
    }
}
