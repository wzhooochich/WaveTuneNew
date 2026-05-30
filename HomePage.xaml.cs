using WaveTuneNew.ViewModels;

namespace WaveTuneNew
{
    public partial class HomePage : ContentPage
    {
        private double _albumsScrollX = 0;
        private double _genresScrollX = 0;
        private const double ScrollStep = 200;

        public HomePage()
        {
            InitializeComponent();
            BindingContext = new HomeViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is HomeViewModel vm)
                await vm.LoadAvatarAsync();
        }

        private async void OnProfileTapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage());
        }

        private async void OnAboutClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AboutPage());
        }

        private async void OnAlbumsScrollLeft(object sender, EventArgs e)
        {
            _albumsScrollX = Math.Max(0, _albumsScrollX - ScrollStep);
            await AlbumsScroll.ScrollToAsync(_albumsScrollX, 0, true);
        }

        private async void OnAlbumsScrollRight(object sender, EventArgs e)
        {
            _albumsScrollX += ScrollStep;
            await AlbumsScroll.ScrollToAsync(_albumsScrollX, 0, true);
        }

        private async void OnGenresScrollLeft(object sender, EventArgs e)
        {
            _genresScrollX = Math.Max(0, _genresScrollX - ScrollStep);
            await GenresScroll.ScrollToAsync(_genresScrollX, 0, true);
        }

        private async void OnGenresScrollRight(object sender, EventArgs e)
        {
            _genresScrollX += ScrollStep;
            await GenresScroll.ScrollToAsync(_genresScrollX, 0, true);
        }
    }
}