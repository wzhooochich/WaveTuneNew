using WaveTuneNew.ViewModels;

namespace WaveTuneNew
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
            BindingContext = new ViewModels.HomeViewModel();
        }
    }
}