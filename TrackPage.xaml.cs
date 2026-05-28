using WaveTuneNew.Models;
using WaveTuneNew.Services;
using WaveTuneNew.ViewModels;
using WaveTuneNew.Views;

namespace WaveTuneNew
{
    public partial class TrackPage : ContentPage
    {
        private readonly WaveDrawable _waveDrawable = new();
        private IDispatcherTimer? _waveTimer;
        private TrackViewModel? _vm;

        public TrackPage(Song song)
        {
            InitializeComponent();
            var player = IPlatformApplication.Current!.Services.GetService<PlayerService>()!;
            _vm = new TrackViewModel(song, player);
            BindingContext = _vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            WaveCanvas.Drawable = _waveDrawable;

            _waveTimer = Application.Current!.Dispatcher.CreateTimer();
            _waveTimer.Interval = TimeSpan.FromMilliseconds(50);
            _waveTimer.Tick += (s, e) =>
            {
                if (_vm?.IsThisTrackPlaying == true)
                {
                    _waveDrawable.Advance(0.08f);
                    WaveCanvas.Invalidate();
                }
            };
            _waveTimer.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _waveTimer?.Stop();
        }
    }
}