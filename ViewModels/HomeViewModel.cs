using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MySqlConnector;
using WaveTuneNew.Models;
using WaveTuneNew.Services;

namespace WaveTuneNew.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        public ObservableCollection<Album> Albums { get; } = new();

        [ObservableProperty]
        private string connectionErrorMessage = string.Empty;

        [ObservableProperty]
        private string avatarUrl = string.Empty;

        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
        public bool HasNoAvatar => string.IsNullOrWhiteSpace(AvatarUrl);

        partial void OnAvatarUrlChanged(string value)
        {
            OnPropertyChanged(nameof(HasAvatar));
            OnPropertyChanged(nameof(HasNoAvatar));
        }

        public HomeViewModel()
        {
            _ = LoadAlbumsAsync();
            _ = LoadAvatarAsync();
        }

        public async Task LoadAvatarAsync()
        {
            var user = SessionService.CurrentUser;
            if (user == null) return;

            const string query = "SELECT avatar_url FROM user_profiles WHERE user_id = @userId";
            var db = new DataBase();
            using var connection = db.getConnection();
            await connection.OpenAsync();
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@userId", user.Id);
            var result = await cmd.ExecuteScalarAsync();
            AvatarUrl = result as string ?? string.Empty;
        }

        private async Task LoadAlbumsAsync()
        {
            ConnectionErrorMessage = string.Empty;
            Albums.Clear();

            const string query = "SELECT id, title, author, picture_url FROM albums";

            try
            {
                var db = new DataBase();
                using var connection = db.getConnection();
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
            catch (MySqlException)
            {
                ConnectionErrorMessage = "Failed to connect to the database. Please try again.";
                await Task.Delay(2000);
                await LoadAlbumsAsync();
            }
            catch (Exception)
            {
                ConnectionErrorMessage = "An unexpected error occurred.";
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