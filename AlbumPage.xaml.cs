using Plugin.Maui.Audio;
using WaveTuneNew.ViewModels;

namespace WaveTuneNew
{
    public partial class AlbumPage : ContentPage
    {
        public AlbumPage(int albumId)
        {
            InitializeComponent();

            var audioManager = AudioManager.Current;
            BindingContext = new ViewModels.AlbumViewModel(albumId, audioManager);
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
