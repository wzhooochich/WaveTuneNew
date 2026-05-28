using Plugin.Maui.Audio;
using WaveTuneNew.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WaveTuneNew.Services
{
    public partial class PlayerService : ObservableObject
    {
        private readonly IAudioManager _audioManager;
        private IAudioPlayer? _player;
        private readonly IDispatcherTimer _timer;

        [ObservableProperty]
        private Song? currentSong;

        [ObservableProperty]
        private double playProgress;

        [ObservableProperty]
        private double volume = 0.5;

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private bool hasCurrentSong;

        [ObservableProperty]
        private bool isVolumeVisible = false;

        private List<Song> _queue = new();
        private int _currentIndex = -1;

        public PlayerService(IAudioManager audioManager)
        {
            _audioManager = audioManager;

            _timer = Application.Current!.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += (s, e) =>
            {
                if (_player is { IsPlaying: true } && _player.Duration > 0)
                    PlayProgress = _player.CurrentPosition / _player.Duration;
            };
        }

        public void SetQueue(IEnumerable<Song> songs, int startIndex = 0)
        {
            _queue = songs.ToList();
            _currentIndex = startIndex;
            PlayCurrent();
        }

        private void PlayCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _queue.Count) return;
            PlaySong(_queue[_currentIndex]);
        }

        private void PlaySong(Song song)
        {
            try
            {
                if (!File.Exists(song.FilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"ФАЙЛ НЕ НАЙДЕН: {song.FilePath}");
                    IsPlaying = false;
                    return;
                }

                if (_player != null)
                {
                    _player.PlaybackEnded -= OnPlaybackEnded;
                    _player.Stop();
                    _player.Dispose();
                }

                CurrentSong = song;
                HasCurrentSong = true;

                var stream = File.OpenRead(song.FilePath);
                _player = _audioManager.CreatePlayer(stream);
                _player.Volume = Volume;
                _player.PlaybackEnded += OnPlaybackEnded;

                PlayProgress = 0;
                _player.Play();
                IsPlaying = true;
                _timer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА ПЛЕЕРА: {ex.Message}");
                IsPlaying = false;
            }
        }

        [RelayCommand]
        public void TogglePlay()
        {
            if (_player == null) return;
            if (_player.IsPlaying)
            {
                _player.Pause();
                IsPlaying = false;
            }
            else
            {
                _player.Play();
                IsPlaying = true;
            }
        }

        [RelayCommand]
        public void PlayNext()
        {
            if (_queue.Count == 0) return;
            _currentIndex = (_currentIndex + 1) >= _queue.Count ? 0 : _currentIndex + 1;
            PlayCurrent();
        }

        [RelayCommand]
        public void PlayPrevious()
        {
            if (_queue.Count == 0) return;
            _currentIndex = (_currentIndex - 1) < 0 ? _queue.Count - 1 : _currentIndex - 1;
            PlayCurrent();
        }

        [RelayCommand]
        public void ShowVolume() => IsVolumeVisible = true;

        [RelayCommand]
        public void HideVolume() => IsVolumeVisible = false;

        partial void OnVolumeChanged(double value)
        {
            if (_player != null)
                _player.Volume = value;
        }

        private void OnPlaybackEnded(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(PlayNext);
        }
    }
}