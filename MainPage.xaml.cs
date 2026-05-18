using WaveTuneNew.ViewModels;

namespace WaveTuneNew
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new LoginViewModel();
        }
    }
}
