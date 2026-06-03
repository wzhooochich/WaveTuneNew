using WaveTuneNew.ViewModels;
using WaveTuneNew.Services;
using WaveTuneNew.Models;
using CommunityToolkit.Maui.Alerts;

namespace WaveTuneNew;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
        BindingContext = new ProfileViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AdminButton.IsVisible = SessionService.CurrentUser?.IsAdmin == true;
    }

    private async void OnAdminPanelClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AdminPage());
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}