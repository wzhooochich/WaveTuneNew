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
        private readonly DataBase _db = new DataBase();
        private readonly int _albumId;
        private readonly PlayerService _player;

        [ObservableProperty]
        private ObservableCollection<Song> _items = new();

        [ObservableProperty]
        private Album _currentAlbum = new();

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

        [RelayCommand]
        private void SelectSong(Song song)
        {
            var index = Items.IndexOf(song);
            _player.SetQueue(Items, index);
        }

        [RelayCommand]
        public async Task GoBack()
        {
            if (Application.Current?.MainPage?.Navigation != null)
                await Application.Current.MainPage.Navigation.PopAsync();
        }
    }
}