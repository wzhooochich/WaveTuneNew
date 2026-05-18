using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MySqlConnector;
using Plugin.Maui.Audio;
using WaveTuneNew.Models;

namespace WaveTuneNew.ViewModels
{
    public partial class AlbumViewModel : ObservableObject
    {
        private readonly DataBase _db = new DataBase();
        private readonly int _albumId;
        private readonly IAudioManager _audioManager;
        private IAudioPlayer? _player;
        private readonly IDispatcherTimer _timer;

        [ObservableProperty]
        private ObservableCollection<Song> _items = new();

        [ObservableProperty]
        private Album _currentAlbum = new();

        [ObservableProperty]
        private Song? currentSong;

        [ObservableProperty]
        private double playProgress;

        [ObservableProperty]
        private double volume = 0.5;

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        public AlbumViewModel(int albumId, IAudioManager audioManager)
        {
            _albumId = albumId;
            _audioManager = audioManager;

            _timer = Application.Current!.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += (s, e) =>
            {
                if (_player is { IsPlaying: true } && _player.Duration > 0)
                {
                    PlayProgress = _player.CurrentPosition / _player.Duration;
                }
            };

            _ = LoadAlbumDataAsync();
        }

        partial void OnVolumeChanged(double value)
        {
            if (_player != null)
            {
                _player.Volume = value;
            }
        }

        private async Task LoadAlbumDataAsync()
        {
            try
            {
                using var connection = _db.getConnection();
                await connection.OpenAsync();

                const string albumQuery = "SELECT title, author, picture_url FROM albums WHERE id = @id";
                using var albumCmd = new MySqlCommand(albumQuery, connection);
                albumCmd.Parameters.AddWithValue("@id", _albumId);

                using var albumReader = await albumCmd.ExecuteReaderAsync();
                if (await albumReader.ReadAsync())
                {
                    CurrentAlbum = new Album
                    {
                        Id = _albumId,
                        Title = albumReader.GetString("title"),
                        Author = albumReader.GetString("author"),
                        PictureUrl = (albumReader["picture_url"] as string ?? "damage.png").Replace("\\", "/")
                    };
                }
                await albumReader.CloseAsync();

                const string songsQuery = "SELECT id, title, author, picture_url, file_path, genre FROM songs WHERE album_id = @albumId";
                using var songsCmd = new MySqlCommand(songsQuery, connection);
                songsCmd.Parameters.AddWithValue("@albumId", _albumId);

                using var songsReader = await songsCmd.ExecuteReaderAsync();
                var tempSongs = new ObservableCollection<Song>();

                while (await songsReader.ReadAsync())
                {
                    tempSongs.Add(new Song
                    {
                        Id = songsReader.GetInt32("id"),
                        Title = songsReader.GetString("title"),
                        Author = songsReader.GetString("author"),
                        PictureUrl = (songsReader["picture_url"] as string ?? "damage.png").Replace("\\", "/"),
                        FilePath = songsReader.GetString("file_path").Replace("\\", "/"),
                        Genre = songsReader["genre"] as string ?? string.Empty,
                        AlbumId = _albumId
                    });
                }
                Items = tempSongs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        [RelayCommand]
        private void SelectSong(Song song)
        {
            CurrentSong = song;
        }

        [RelayCommand]
        private void TogglePlay()
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
        public async Task GoBack()
        {
            if (Application.Current?.MainPage?.Navigation != null)
            {
                await Application.Current.MainPage.Navigation.PopAsync();
            }
        }

        [RelayCommand]
        public void PlayNext()
        {
            if (CurrentSong == null || Items.Count == 0) return;

            int currentIndex = Items.IndexOf(CurrentSong);
            int nextIndex = (currentIndex + 1) >= Items.Count ? 0 : currentIndex + 1;

            CurrentSong = Items[nextIndex];
        }

        [RelayCommand]
        public void PlayPrevious()
        {
            if (CurrentSong == null || Items.Count == 0) return;

            int currentIndex = Items.IndexOf(CurrentSong);
            int prevIndex = (currentIndex - 1) < 0 ? Items.Count - 1 : currentIndex - 1;

            CurrentSong = Items[prevIndex];
        }

        partial void OnCurrentSongChanged(Song? value)
        {
            if (value is null) return;

            try
            {
                if (!File.Exists(value.FilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"!!! ФАЙЛ НЕ НАЙДЕН по пути: {value.FilePath}");
                    IsPlaying = false;
                    return;
                }

                if (_player != null)
                {
                    _player.PlaybackEnded -= OnPlaybackEnded;
                    _player.Stop();
                    _player.Dispose();
                }

                var stream = File.OpenRead(value.FilePath);
                _player = _audioManager.CreatePlayer(stream);

                _player.Volume = Volume;
                _player.PlaybackEnded += OnPlaybackEnded;

                PlayProgress = 0;
                _player.Play();
                IsPlaying = true;
                _timer.Start();

                System.Diagnostics.Debug.WriteLine($">>> Сейчас играет: {value.Title}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! ОШИБКА ПЛЕЕРА: {ex.Message}");
                IsPlaying = false;
            }
        }

        private void OnPlaybackEnded(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PlayNext();
            });
        }
    }
}