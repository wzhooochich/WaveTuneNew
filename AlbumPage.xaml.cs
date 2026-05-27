using WaveTuneNew.Services;
using WaveTuneNew.ViewModels;

namespace WaveTuneNew
{
    public partial class AlbumPage : ContentPage
    {
        public AlbumPage(int albumId)
        {
            InitializeComponent();
            var player = IPlatformApplication.Current!.Services.GetService<PlayerService>()!;
            BindingContext = new AlbumViewModel(albumId, player);
        }
    }
}