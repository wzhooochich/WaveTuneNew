using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MySqlConnector;
using WaveTuneNew.Models;

namespace WaveTuneNew.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        public ObservableCollection<Album> Albums { get; } = new();
        private readonly DataBase _db = new DataBase();

        public HomeViewModel()
        {
            _ = LoadAlbumsAsync();
        }

        private async Task LoadAlbumsAsync()
        {
            Albums.Clear();
            const string query = "SELECT id, title, author, picture_url FROM albums";
            using var connection = _db.getConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Albums.Add(new Album
                {
                    Id = reader.GetInt32("id"),
                    Title = reader.GetString("title"),
                    Author = reader.GetString("author"),
                    PictureUrl = reader["picture_url"] as string ?? string.Empty
                });
            }
        }

        [RelayCommand]
        private async Task GoToAlbum(Album album)
        {
            if (album == null) return;
            var navigation = Application.Current?.MainPage?.Navigation;

            if (navigation != null)
            {
                await navigation.PushAsync(new AlbumPage(album.Id));
            }
        }
    }
}
