using WaveTuneNew.ViewModels;

namespace WaveTuneNew
{
    public partial class GenrePage : ContentPage
    {
        public GenrePage(GenreItem genre)
        {
            InitializeComponent();
            BindingContext = new GenreViewModel(genre);
        }
    }
}