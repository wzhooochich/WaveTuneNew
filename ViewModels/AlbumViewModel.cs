using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MySqlConnector;
using WaveTuneNew.Models;
using WaveTuneNew.Services;

namespace WaveTuneNew.ViewModels
{
    public partial class AlbumViewModel : ObservableObject
    {
        private readonly int _albumId;
        private readonly PlayerService _player;

        [ObservableProperty]
        private ObservableCollection<Song> _items = new();

        [ObservableProperty]
        private Album _currentAlbum = new();

        [ObservableProperty]
        private bool isAlbumLiked;

        public AlbumViewModel(int albumId, PlayerService player)
        {
            _albumId = albumId;
            _player = player;
            _ = LoadAlbumDataAsync();
        }

        private async Task LoadAlbumDataAsync()
        {
            try
            {
                var db = new DataBase();
                using var connection = db.getConnection();
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

                await CheckIfAlbumLikedAsync(connection);

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
                        Genre = Song.ParseGenre(songsReader["genre"] as string),
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

        private async Task CheckIfAlbumLikedAsync(MySqlConnection connection)
        {
            var user = SessionService.CurrentUser;
            if (user == null) return;

            const string query = "SELECT COUNT(*) FROM user_liked_albums WHERE user_id = @userId AND album_id = @albumId";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@userId", user.Id);
            cmd.Parameters.AddWithValue("@albumId", _albumId);

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            IsAlbumLiked = count > 0;
        }

        [RelayCommand]
        private async Task ToggleLikeAlbumAsync()
        {
            var user = SessionService.CurrentUser;
            if (user == null) return;

            try
            {
                var db = new DataBase();
                using var connection = db.getConnection();
                await connection.OpenAsync();

                if (IsAlbumLiked)
                {
                    const string deleteQuery = "DELETE FROM user_liked_albums WHERE user_id = @userId AND album_id = @albumId";
                    using var cmd = new MySqlCommand(deleteQuery, connection);
                    cmd.Parameters.AddWithValue("@userId", user.Id);
                    cmd.Parameters.AddWithValue("@albumId", _albumId);
                    await cmd.ExecuteNonQueryAsync();
                    IsAlbumLiked = false;
                }
                else
                {
                    const string insertQuery = "INSERT INTO user_liked_albums (user_id, album_id, liked_at) VALUES (@userId, @albumId, NOW())";
                    using var cmd = new MySqlCommand(insertQuery, connection);
                    cmd.Parameters.AddWithValue("@userId", user.Id);
                    cmd.Parameters.AddWithValue("@albumId", _albumId);
                    await cmd.ExecuteNonQueryAsync();
                    IsAlbumLiked = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        [RelayCommand]
        private void SelectSong(Song song)
        {
            var index = Items.IndexOf(song);
            _player.SetQueue(Items, index);
        }

        [RelayCommand]
        private void HoverSong(Song song)
        {
            song.IsHovered = true;
        }

        [RelayCommand]
        private void UnhoverSong(Song song)
        {
            song.IsHovered = false;
        }

        [RelayCommand]
        public async Task GoBack()
        {
            if (Application.Current?.MainPage?.Navigation != null)
                await Application.Current.MainPage.Navigation.PopAsync();
        }
    }
}