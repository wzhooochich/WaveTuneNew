using Plugin.Maui.Audio;
using WaveTuneNew.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySqlConnector;

namespace WaveTuneNew.Services
{
    public partial class PlayerService : ObservableObject 
    {
        // ИНТЕРФЕЙСЫ (встроенные)
        private readonly IAudioManager _audioManager;
        private IAudioPlayer? _player;
        private readonly IDispatcherTimer _timer;


        [ObservableProperty] private Song? currentSong;
        [ObservableProperty] private double playProgress;
        [ObservableProperty] private double volume = 0.5;
        [ObservableProperty] private bool isPlaying;
        [ObservableProperty] private bool hasCurrentSong;
        [ObservableProperty] private bool isVolumeVisible = false;
        [ObservableProperty] private bool isCurrentSongLiked;

        private List<Song> _queue = new();
        private int _currentIndex = -1;

        public PlayerService(IAudioManager audioManager)
        {
            _audioManager = audioManager;

            _timer = Application.Current!.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            //обработчик события Tick таймера
            _timer.Tick += (s, e) =>
            {
                if (_player is { IsPlaying: true } && _player.Duration > 0)
                    PlayProgress = _player.CurrentPosition / _player.Duration;
            };
        }

        public void SetQueue(IEnumerable<Song> songs, int startIndex = 0) // ИНТЕРФЕЙСЫ 
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
                    IsPlaying = false;
                    return;
                }

                if (_player != null)
                {
                    // ДЕЛЕГАТЫ/СОБЫТИЯ — отписка от события перед уничтожением плеера
                    _player.PlaybackEnded -= OnPlaybackEnded;
                    _player.Stop();
                    _player.Dispose();
                    _player = null;
                }

                CurrentSong = song;
                HasCurrentSong = true;

                // ПОТОКИ — Task.Run запускает задачу в фоновом потоке
                _ = Task.Run(() => CheckIfSongLikedAsync(song.Id));

                var stream = File.OpenRead(song.FilePath);
                // ИНТЕРФЕЙСЫ — IAudioManager создаёт IAudioPlayer
                _player = _audioManager.CreatePlayer(stream);
                _player.Volume = Volume;
                // ДЕЛЕГАТЫ/СОБЫТИЯ — подписка на событие окончания воспроизведения
                _player.PlaybackEnded += OnPlaybackEnded;

                PlayProgress = 0;
                _player.Play();
                IsPlaying = true;
                _timer.Start();
            }
            catch (Exception ex) // ОБРАБОТКА ИСКЛЮЧЕНИЙ
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА ПЛЕЕРА: {ex.Message}");
                IsPlaying = false;
            }
        }

        private async Task CheckIfSongLikedAsync(int songId) // ПОТОКИ — async Task
        {
            var user = SessionService.CurrentUser;
            if (user == null)
            {
                // ПОТОКИ — возврат результата на UI-поток из фонового
                MainThread.BeginInvokeOnMainThread(() => IsCurrentSongLiked = false);
                return;
            }

            try 
            {
                var db = new DataBase();
                using var connection = db.getConnection();
                await connection.OpenAsync();

               
                const string query = "SELECT COUNT(*) FROM user_liked_songs WHERE user_id = @userId AND song_id = @songId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@userId", user.Id);
                cmd.Parameters.AddWithValue("@songId", songId);

                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                MainThread.BeginInvokeOnMainThread(() => IsCurrentSongLiked = count > 0);
            }
            catch
            {
                MainThread.BeginInvokeOnMainThread(() => IsCurrentSongLiked = false);
            }
        }

        [RelayCommand]
        public async Task ToggleLikeCurrentSong() 
        {
            var user = SessionService.CurrentUser;
            if (user == null || CurrentSong == null) return;

            try 
            {
                var db = new DataBase();
                using var connection = db.getConnection();
                await connection.OpenAsync();

                if (IsCurrentSongLiked)
                {
                    const string deleteQuery = "DELETE FROM user_liked_songs WHERE user_id = @userId AND song_id = @songId";
                    using var cmd = new MySqlCommand(deleteQuery, connection);
                    cmd.Parameters.AddWithValue("@userId", user.Id);
                    cmd.Parameters.AddWithValue("@songId", CurrentSong.Id);
                    await cmd.ExecuteNonQueryAsync();
                    IsCurrentSongLiked = false;
                }
                else
                {
                    const string insertQuery = "INSERT INTO user_liked_songs (user_id, song_id, liked_at) VALUES (@userId, @songId, NOW())";
                    using var cmd = new MySqlCommand(insertQuery, connection);
                    cmd.Parameters.AddWithValue("@userId", user.Id);
                    cmd.Parameters.AddWithValue("@songId", CurrentSong.Id);
                    await cmd.ExecuteNonQueryAsync();
                    IsCurrentSongLiked = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
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

        // ДЕЛЕГАТЫ/СОБЫТИЯ — обработчик события PlaybackEnded, вызывает PlayNext 
        private void OnPlaybackEnded(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(PlayNext); // ПОТОКИ — передача метода как делегата
        }
    }
}