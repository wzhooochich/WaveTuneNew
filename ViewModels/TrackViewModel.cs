using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WaveTuneNew.Models;
using WaveTuneNew.Services;

namespace WaveTuneNew.ViewModels
{
    public partial class TrackViewModel : ObservableObject
    {
        private readonly PlayerService _player;

        [ObservableProperty]
        private Song song;

        public bool IsThisTrackPlaying => _player.CurrentSong?.Id == Song.Id && _player.IsPlaying;
        public double PlayProgress => IsThisTrackPlaying ? _player.PlayProgress : 0;

        public TrackViewModel(Song song, PlayerService player)
        {
            this.song = song;
            _player = player;

            _player.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlayerService.PlayProgress) ||
                    e.PropertyName == nameof(PlayerService.IsPlaying) ||
                    e.PropertyName == nameof(PlayerService.CurrentSong))
                {
                    OnPropertyChanged(nameof(IsThisTrackPlaying));
                    OnPropertyChanged(nameof(PlayProgress));
                }
            };
        }

        public bool ShouldAnimateWave => IsThisTrackPlaying;

        [RelayCommand]
        private void PlayThis()
        {
            if (_player.CurrentSong?.Id == Song.Id)
                _player.TogglePlayCommand.Execute(null);
            else
                _player.SetQueue(new List<Song> { Song }, 0);
        }

        [RelayCommand]
        private async Task GoBack()
        {
            if (Application.Current?.MainPage?.Navigation != null)
                await Application.Current.MainPage.Navigation.PopAsync();
        }

        public string GenreDisplay => Song.Genre switch
        {
            Genre.Trap => "Trap",
            Genre.Hyperpop => "Hyperpop",
            Genre.NewJazz => "New Jazz",
            Genre.CloudRap => "Cloud Rap",
            _ => "Unknown"
        };
    }
}