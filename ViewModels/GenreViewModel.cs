using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MySqlConnector;
using WaveTuneNew.Models;

namespace WaveTuneNew.ViewModels
{
    public partial class GenreViewModel : ObservableObject
    {
        public ObservableCollection<Song> Songs { get; } = new();

        [ObservableProperty]
        private string genreName = string.Empty;

        private readonly Genre _genre;

        public GenreViewModel(GenreItem genreItem)
        {
            _genre = genreItem.Genre;
            GenreName = genreItem.Name;
            _ = LoadSongsAsync();
        }

        private async Task LoadSongsAsync()
        {
            Songs.Clear();

            var genreStr = _genre switch
            {
                Genre.Trap => "trap",
                Genre.Hyperpop => "hyperpop",
                Genre.NewJazz => "new jazz",
                Genre.CloudRap => "cloud rap",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(genreStr)) return;

            const string query = "SELECT id, title, author, picture_url, file_path, genre FROM songs WHERE genre = @genre LIMIT 30";

            try
            {
                var db = new DataBase();
                using var connection = db.getConnection();
                await connection.OpenAsync();
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@genre", genreStr);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Songs.Add(new Song
                    {
                        Id = reader.GetInt32("id"),
                        Title = reader.GetString("title"),
                        Author = reader.GetString("author"),
                        PictureUrl = (reader["picture_url"] as string ?? string.Empty).Replace("\\", "/"),
                        FilePath = (reader["file_path"] as string ?? string.Empty).Replace("\\", "/"),
                        Genre = Song.ParseGenre(reader["genre"] as string)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        [RelayCommand]
        private async Task GoBack()
        {
            if (Application.Current?.MainPage?.Navigation != null)
                await Application.Current.MainPage.Navigation.PopAsync();
        }
    }
}