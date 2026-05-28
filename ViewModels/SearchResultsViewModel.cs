using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySqlConnector;
using System.Collections.ObjectModel;
using WaveTuneNew.Models;
using WaveTuneNew.Services;

namespace WaveTuneNew.ViewModels
{
    public partial class SearchResultsViewModel : ObservableObject
    {
        public ObservableCollection<Album> Albums { get; } = new();
        public ObservableCollection<Song> Songs { get; } = new();
        public ObservableCollection<string> Authors { get; } = new();

        [ObservableProperty]
        private string searchQuery = string.Empty;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private bool hasNoResults = false;

        private readonly PlayerService _player;

        public SearchResultsViewModel(string query, PlayerService player)
        {
            SearchQuery = query;
            _player = player;
            _ = SearchAsync(query);
        }

        private async Task SearchAsync(string query)
        {
            IsLoading = true;
            Albums.Clear();
            Songs.Clear();
            Authors.Clear();

            try
            {
                var db = new DataBase();
                using var connection = db.getConnection();
                await connection.OpenAsync();

                const string albumQuery = "SELECT id, title, author, picture_url FROM albums WHERE title LIKE @q OR author LIKE @q LIMIT 10";
                using (var cmd = new MySqlCommand(albumQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@q", $"%{query}%");
                    using var reader = await cmd.ExecuteReaderAsync();
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

                const string songQuery = "SELECT id, title, author, picture_url, file_path, genre FROM songs WHERE title LIKE @q OR author LIKE @q LIMIT 20";
                using (var cmd = new MySqlCommand(songQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@q", $"%{query}%");
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

                const string authorQuery = "SELECT DISTINCT author FROM songs WHERE author LIKE @q LIMIT 5";
                using (var cmd = new MySqlCommand(authorQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@q", $"%{query}%");
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        Authors.Add(reader.GetString("author"));
                }

                HasNoResults = Albums.Count == 0 && Songs.Count == 0 && Authors.Count == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GoToAlbum(Album album)
        {
            if (album == null) return;
            await Application.Current!.MainPage!.Navigation.PushAsync(new AlbumPage(album.Id));
        }

        [RelayCommand]
        private async Task GoToTrack(Song song)
        {
            if (song == null) return;
            await Application.Current!.MainPage!.Navigation.PushAsync(new TrackPage(song));
        }

        [RelayCommand]
        private async Task GoBack()
        {
            if (Application.Current?.MainPage?.Navigation != null)
                await Application.Current.MainPage.Navigation.PopAsync();
        }
    }
}