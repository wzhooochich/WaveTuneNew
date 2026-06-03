using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySqlConnector;
using System.Collections.ObjectModel;
using WaveTuneNew.Models;
using WaveTuneNew.Services;

namespace WaveTuneNew.ViewModels
{
    public partial class AdminViewModel : ObservableObject
    {
        private readonly DataBase _db = new();

        [ObservableProperty] private ObservableCollection<Album> _albums = new();
        [ObservableProperty] private ObservableCollection<Song> _songs = new();
        [ObservableProperty] private ObservableCollection<User> _users = new();
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _isLoading = false;
        [ObservableProperty] private string _newAlbumTitle = string.Empty;
        [ObservableProperty] private string _newAlbumAuthor = string.Empty;
        [ObservableProperty] private string _newSongTitle = string.Empty;
        [ObservableProperty] private string _newSongAuthor = string.Empty;
        [ObservableProperty] private string _newSongGenre = string.Empty;
        [ObservableProperty] private string _newSongFilePath = string.Empty;

        public AdminViewModel() => _ = LoadAllDataAsync();

        private async Task LoadAllDataAsync()
        {
            IsLoading = true;
            try
            {
                _db.openConnection();
                var conn = _db.getConnection();

                using var cmdA = new MySqlCommand("SELECT id, title, author, picture_url, genre FROM albums", conn);
                using var rA = await cmdA.ExecuteReaderAsync();
                var tempAlbums = new ObservableCollection<Album>();
                while (await rA.ReadAsync())
                    tempAlbums.Add(new Album { Id = rA.GetInt32("id"), Title = rA.GetString("title"), Author = rA.GetString("author"), PictureUrl = rA["picture_url"] as string ?? "damage.png", Genre = rA["genre"] as string ?? "Unknown" });
                Albums = tempAlbums;
                await rA.CloseAsync();

                using var cmdS = new MySqlCommand("SELECT id, title, author, picture_url, file_path, genre, album_id, user_id FROM songs", conn);
                using var rS = await cmdS.ExecuteReaderAsync();
                var tempSongs = new ObservableCollection<Song>();
                while (await rS.ReadAsync())
                    tempSongs.Add(new Song { Id = rS.GetInt32("id"), Title = rS.GetString("title"), Author = rS.GetString("author"), PictureUrl = rS["picture_url"] as string ?? "damage.png", FilePath = rS["file_path"] as string ?? "", Genre = Song.ParseGenre(rS["genre"] as string), AlbumId = rS["album_id"] as int?, UserId = rS["user_id"] as int? });
                Songs = tempSongs;
                await rS.CloseAsync();

                using var cmdU = new MySqlCommand("SELECT id, login, password FROM users", conn);
                using var rU = await cmdU.ExecuteReaderAsync();
                var tempUsers = new ObservableCollection<User>();
                while (await rU.ReadAsync())
                    tempUsers.Add(new User { Id = rU.GetInt32("id"), Login = rU.GetString("login"), Password = rU.GetString("password") });
                Users = tempUsers;

                StatusMessage = "Данные загружены";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { _db.closeConnection(); IsLoading = false; }
        }

        [RelayCommand] private async Task RefreshDataAsync() => await LoadAllDataAsync();
        [RelayCommand] public async Task GoBackAsync() => await (Application.Current?.MainPage?.Navigation?.PopAsync() ?? Task.CompletedTask);

        [RelayCommand]
        private async Task AddAlbumAsync()
        {
            if (string.IsNullOrWhiteSpace(NewAlbumTitle) || string.IsNullOrWhiteSpace(NewAlbumAuthor))
            { StatusMessage = "Заполните название и автора"; return; }
            await ExecAddAlbum(NewAlbumTitle, NewAlbumAuthor, "damage.png", "Unknown");
            NewAlbumTitle = NewAlbumAuthor = string.Empty;
        }

        [RelayCommand] private async Task DeleteAlbumAsync(int id) => await ExecDelete("DELETE FROM albums WHERE id = @id", id, "Альбом удалён");

        private async Task ExecAddAlbum(string t, string a, string p, string g)
        {
            try
            {
                _db.openConnection();
                using var cmd = new MySqlCommand("INSERT INTO albums (title, author, picture_url, genre) VALUES (@t,@a,@p,@g)", _db.getConnection());
                cmd.Parameters.AddWithValue("@t", t); cmd.Parameters.AddWithValue("@a", a);
                cmd.Parameters.AddWithValue("@p", p); cmd.Parameters.AddWithValue("@g", g);
                await cmd.ExecuteNonQueryAsync();
                StatusMessage = "Альбом добавлен";
                await LoadAllDataAsync();
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { _db.closeConnection(); }
        }

        [RelayCommand]
        private async Task AddSongAsync()
        {
            if (string.IsNullOrWhiteSpace(NewSongTitle)) { StatusMessage = "Введите название трека"; return; }
            await ExecAddSong(NewSongTitle, NewSongAuthor, "damage.png", NewSongFilePath, NewSongGenre, null, null);
            NewSongTitle = NewSongAuthor = NewSongGenre = NewSongFilePath = string.Empty;
        }

        [RelayCommand] private async Task DeleteSongAsync(int id) => await ExecDelete("DELETE FROM songs WHERE id = @id", id, "Трек удалён");

        private async Task ExecAddSong(string t, string a, string p, string f, string g, int? aid, int? uid)
        {
            try
            {
                _db.openConnection();
                using var cmd = new MySqlCommand("INSERT INTO songs (title, author, picture_url, file_path, genre, album_id, user_id) VALUES (@t,@a,@p,@f,@g,@aid,@uid)", _db.getConnection());
                cmd.Parameters.AddWithValue("@t", t); cmd.Parameters.AddWithValue("@a", a);
                cmd.Parameters.AddWithValue("@p", p); cmd.Parameters.AddWithValue("@f", f);
                cmd.Parameters.AddWithValue("@g", g); cmd.Parameters.AddWithValue("@aid", aid ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@uid", uid ?? (object)DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
                StatusMessage = "Трек добавлен";
                await LoadAllDataAsync();
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { _db.closeConnection(); }
        }

        [RelayCommand]
        private async Task DeleteUserAsync(int id)
        {
            if (SessionService.CurrentUser?.Id == id) { StatusMessage = "Нельзя удалить себя"; return; }
            await ExecDelete("DELETE FROM users WHERE id = @id", id, "Пользователь удалён");
        }

        private async Task ExecDelete(string query, int id, string msg)
        {
            try
            {
                _db.openConnection();
                using var cmd = new MySqlCommand(query, _db.getConnection());
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                StatusMessage = msg;
                await LoadAllDataAsync();
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { _db.closeConnection(); }
        }
    }
}