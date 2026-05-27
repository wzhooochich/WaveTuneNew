using WaveTuneNew.Services;
using WaveTuneNew.ViewModels;

namespace WaveTuneNew
{
    public partial class GenrePage : ContentPage
    {
        public GenrePage(GenreItem genre)
        {
            InitializeComponent();
            var player = IPlatformApplication.Current!.Services.GetService<PlayerService>()!;
            BindingContext = new GenreViewModel(genre, player);
        }
    }
}