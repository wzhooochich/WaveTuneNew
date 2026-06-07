using WaveTuneNew.Models;
using WaveTuneNew.Services;

namespace WaveTuneNew.Views
{
    public partial class PlayerBarView : ContentView
    {
        public PlayerBarView()
        {
            InitializeComponent();
            BindingContext = App.Current?.Handler?.MauiContext?.Services.GetService<PlayerService>();
        }

        private async void OnSongTitleTapped(object sender, TappedEventArgs e)
        {
            var player = BindingContext as PlayerService;
            if (player?.CurrentSong == null) return;

            var navigation = Application.Current?.MainPage?.Navigation;
            if (navigation != null)
                await navigation.PushAsync(new TrackPage(player.CurrentSong));
        }
    }
}