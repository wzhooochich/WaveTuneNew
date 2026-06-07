namespace WaveTuneNew.Views
{
    public partial class PlayerBarView : ContentView
    {
        public PlayerBarView()
        {
            InitializeComponent();
            BindingContext = App.Current?.Handler?.MauiContext?.Services.GetService<Services.PlayerService>();
        }
    }
}