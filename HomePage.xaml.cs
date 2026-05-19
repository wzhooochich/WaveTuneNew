using WaveTuneNew.ViewModels;

namespace WaveTuneNew
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
            BindingContext = new HomeViewModel();
        }

        private async void OnProfileTapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage());
        }
    }
}