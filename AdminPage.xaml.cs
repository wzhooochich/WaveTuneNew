using WaveTuneNew.ViewModels;
using WaveTuneNew.Services;

namespace WaveTuneNew;

public partial class AdminPage : ContentPage
{
    public AdminPage()
    {
        if (SessionService.CurrentUser?.IsAdmin != true)
        {
            DisplayAlert("Доступ запрещён", "Только для администраторов", "ОК");
            if (Navigation.NavigationStack.Count > 1)
                Navigation.PopAsync();
            else
                Shell.Current.GoToAsync("//MainPage");
            return;
        }

        InitializeComponent();
        BindingContext = new AdminViewModel();
    }
}