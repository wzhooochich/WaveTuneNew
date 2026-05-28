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
        private readonly List<Album> _allAlbums = new();

        public ObservableCollection<Album> Albums { get; } = new();
        public ObservableCollection<GenreItem> Genres { get; } = new();
        public ObservableCollection<SearchSuggestion> Suggestions { get; } = new();

        [ObservableProperty]
        private string connectionErrorMessage = string.Empty;

        [ObservableProperty]
        private string avatarUrl = string.Empty;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private bool showSuggestions = false;

        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
        public bool HasNoAvatar => string.IsNullOrWhiteSpace(AvatarUrl);

        partial void OnAvatarUrlChanged(string value)
        {
            OnPropertyChanged(nameof(HasAvatar));
            OnPropertyChanged(nameof(HasNoAvatar));
        }

        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Suggestions.Clear();
                ShowSuggestions = false;
                return;
            }
            _ = LoadSuggestionsAsync(value);
        }

        private async Task LoadSuggestionsAsync(string query)
        {
            Suggestions.Clear();

            try
            {
                var db = new DataBase();
                using var connection = db.getConnection();
                await connection.OpenAsync();

                const string sql = @"
                    (SELECT 'album' as type, id, title as name, author, picture_url FROM albums
                     WHERE title LIKE @q OR author LIKE @q LIMIT 2)
                    UNION ALL
                    (SELECT 'track' as type, id, title as name, author, picture_url FROM songs
                     WHERE title LIKE @q OR author LIKE @q LIMIT 2)
                    LIMIT 4";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@q", $"%{query}%");
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Suggestions.Add(new SearchSuggestion
                    {
                        Type = reader.GetString("type"),
                        Id = reader.GetInt32("id"),
                        Name = reader.GetString("name"),
                        Author = reader.GetString("author"),
                        PictureUrl = (reader["picture_url"] as string ?? string.Empty).Replace("\\", "/")
                    });
                }

                ShowSuggestions = Suggestions.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public HomeViewModel()
        {
            _ = LoadAlbumsAsync();
            _ = LoadAvatarAsync();
            _ = LoadGenresAsync();
        }

        private async Task LoadGenresAsync()
        {
            var genreList = new[]
            {
                new { Name = "Trap", Genre = Genre.Trap, DbValue = "trap", Color = "#1a1a2e" },
                new { Name = "Hyperpop", Genre = Genre.Hyperpop, DbValue = "hyperpop", Color = "#16213e" },
                new { Name = "New Jazz", Genre = Genre.NewJazz, DbValue = "new jazz", Color = "#0f3460" },
                new { Name = "Cloud Rap", Genre = Genre.CloudRap, DbValue = "cloud rap", Color = "#2d1b4e" }
            };

            try
            {
                var db = new DataBase();
                using var connection = db.getConnection();
                await connection.OpenAsync();

                foreach (var g in genreList)
                {
                    const string query = "SELECT picture_url FROM songs WHERE genre = @genre ORDER BY RAND() LIMIT 1";
                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@genre", g.DbValue);
                    var result = await cmd.ExecuteScalarAsync();
                    var pic = (result as string ?? string.Empty).Replace("\\", "/");

                    Genres.Add(new GenreItem
                    {
                        Name = g.Name,
                        Genre = g.Genre,
                        Color = g.Color,
                        PictureUrl = pic
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
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
            _allAlbums.Clear();
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
                    _allAlbums.Add(new Album
                    {
                        Id = reader.GetInt32("id"),
                        Title = reader.GetString("title"),
                        Author = reader.GetString("author"),
                        PictureUrl = reader["picture_url"] as string ?? string.Empty
                    });
                }
                foreach (var a in _allAlbums) Albums.Add(a);
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
                await navigation.PushAsync(new AlbumPage(album.Id));
        }

        [RelayCommand]
        private async Task GoToGenre(GenreItem genre)
        {
            if (genre == null) return;
            var navigation = Application.Current?.MainPage?.Navigation;
            if (navigation != null)
                await navigation.PushAsync(new GenrePage(genre));
        }

        [RelayCommand]
        public async Task GoToSearchResults()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return;
            ShowSuggestions = false;
            var navigation = Application.Current?.MainPage?.Navigation;
            if (navigation != null)
                await navigation.PushAsync(new SearchResultsPage(SearchText));
        }

        [RelayCommand]
        public async Task SelectSuggestion(SearchSuggestion suggestion)
        {
            if (suggestion == null) return;
            ShowSuggestions = false;
            SearchText = string.Empty;

            var navigation = Application.Current?.MainPage?.Navigation;
            if (navigation == null) return;

            if (suggestion.Type == "album")
            {
                await navigation.PushAsync(new AlbumPage(suggestion.Id));
            }
            else
            {
                var db = new DataBase();
                using var connection = db.getConnection();
                await connection.OpenAsync();
                const string q = "SELECT id, title, author, picture_url, file_path, genre FROM songs WHERE id = @id";
                using var cmd = new MySqlCommand(q, connection);
                cmd.Parameters.AddWithValue("@id", suggestion.Id);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var song = new Song
                    {
                        Id = reader.GetInt32("id"),
                        Title = reader.GetString("title"),
                        Author = reader.GetString("author"),
                        PictureUrl = (reader["picture_url"] as string ?? string.Empty).Replace("\\", "/"),
                        FilePath = (reader["file_path"] as string ?? string.Empty).Replace("\\", "/"),
                        Genre = Song.ParseGenre(reader["genre"] as string)
                    };
                    await navigation.PushAsync(new TrackPage(song));
                }
            }
        }
    }

    public class SearchSuggestion
    {
        public string Type { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string PictureUrl { get; set; } = string.Empty;
        public string TypeLabel => Type == "album" ? "Альбом" : "Трек";
        public string TypeColor => Type == "album" ? "#602191" : "#51ADED";
    }

    public class GenreItem
    {
        public string Name { get; set; } = string.Empty;
        public Genre Genre { get; set; }
        public string Color { get; set; } = "#333333";
        public string PictureUrl { get; set; } = string.Empty;
        public bool HasPicture => !string.IsNullOrWhiteSpace(PictureUrl);
    }
}