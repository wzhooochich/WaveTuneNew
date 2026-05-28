using WaveTuneNew.Services;
using WaveTuneNew.ViewModels;

namespace WaveTuneNew
{
    public partial class SearchResultsPage : ContentPage
    {
        public SearchResultsPage(string query)
        {
            InitializeComponent();
            var player = IPlatformApplication.Current!.Services.GetService<PlayerService>()!;
            BindingContext = new SearchResultsViewModel(query, player);
        }
    }
}