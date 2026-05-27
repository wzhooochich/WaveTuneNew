using WaveTuneNew.Services;

namespace WaveTuneNew.Views
{
    public partial class PlayerBarView : ContentView
    {
        public PlayerBarView()
        {
            InitializeComponent();
            BindingContext = IPlatformApplication.Current?.Services.GetService<PlayerService>();
        }
    }
}