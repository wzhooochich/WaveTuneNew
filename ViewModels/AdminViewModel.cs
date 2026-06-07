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

        [ObservableProperty] private ObservableCollection<Song> _songs = new();
        [ObservableProperty] private ObservableCollection<User> _users = new();
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _isLoading = false;
        [ObservableProperty] private string _searchSongTitle = string.Empty;
        [ObservableProperty] private string _newAdminLogin = string.Empty;

        public AdminViewModel() => _ = LoadDataAsync();

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                _db.openConnection();
                var conn = _db.getConnection();

                using var cmdS = new MySqlCommand("SELECT id, title, author, picture_url, file_path, genre, album_id, user_id FROM songs ORDER BY title", conn);
                using var rS = await cmdS.ExecuteReaderAsync();
                var tempSongs = new ObservableCollection<Song>();
                while (await rS.ReadAsync())
                    tempSongs.Add(new Song { Id = rS.GetInt32("id"), Title = rS.GetString("title"), Author = rS.GetString("author"), PictureUrl = rS["picture_url"] as string ?? "damage.png", FilePath = rS["file_path"] as string ?? "", Genre = Song.ParseGenre(rS["genre"] as string), AlbumId = rS["album_id"] as int?, UserId = rS["user_id"] as int? });
                Songs = tempSongs;
                await rS.CloseAsync();

                using var cmdU = new MySqlCommand("SELECT id, login FROM users WHERE is_admin = 1 ORDER BY login", conn);
                using var rU = await cmdU.ExecuteReaderAsync();
                var tempUsers = new ObservableCollection<User>();
                while (await rU.ReadAsync())
                    tempUsers.Add(new User { Id = rU.GetInt32("id"), Login = rU.GetString("login") });
                Users = tempUsers;

                StatusMessage = $"Загружено: {Songs.Count} треков, {Users.Count} админов";
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            finally { _db.closeConnection(); IsLoading = false; }
        }

        [RelayCommand] private async Task RefreshDataAsync() => await LoadDataAsync();
        [RelayCommand] public async Task GoBackAsync() => await (Application.Current?.MainPage?.Navigation?.PopAsync() ?? Task.CompletedTask);

        [RelayCommand]
        private async Task SearchSongsAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchSongTitle))
            {
                await LoadDataAsync();
                return;
            }

            IsLoading = true;
            try
            {
                _db.openConnection();
                using var cmd = new MySqlCommand("SELECT id, title, author, picture_url, file_path, genre, album_id, user_id FROM songs WHERE title LIKE @title ORDER BY title", _db.getConnection());
                cmd.Parameters.AddWithValue("@title", "%" + SearchSongTitle + "%");
                using var reader = await cmd.ExecuteReaderAsync();
                var tempSongs = new ObservableCollection<Song>();
                while (await reader.ReadAsync())
                    tempSongs.Add(new Song { Id = reader.GetInt32("id"), Title = reader.GetString("title"), Author = reader.GetString("author"), PictureUrl = reader["picture_url"] as string ?? "damage.png", FilePath = reader["file_path"] as string ?? "", Genre = Song.ParseGenre(reader["genre"] as string), AlbumId = reader["album_id"] as int?, UserId = reader["user_id"] as int? });
                Songs = tempSongs;
                StatusMessage = $"Найдено: {Songs.Count}";
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            finally { _db.closeConnection(); IsLoading = false; }
        }

        [RelayCommand]
        private async Task DeleteSongAsync(int id)
        {
            var confirm = await (Application.Current?.MainPage?.DisplayAlert("Удалить трек?", "Вы уверены?", "Да", "Нет") ?? Task.FromResult(false));
            if (!confirm) return;

            try
            {
                _db.openConnection();
                using var cmd = new MySqlCommand("DELETE FROM songs WHERE id = @id", _db.getConnection());
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                StatusMessage = "Трек удалён";
                await LoadDataAsync();
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            finally { _db.closeConnection(); }
        }

        [RelayCommand]
        private async Task AddAdminAsync()
        {
            if (string.IsNullOrWhiteSpace(NewAdminLogin))
            {
                StatusMessage = "Введите логин пользователя";
                return;
            }

            try
            {
                _db.openConnection();
                var conn = _db.getConnection();

                using var checkCmd = new MySqlCommand("SELECT id, login FROM users WHERE login = @login", conn);
                checkCmd.Parameters.AddWithValue("@login", NewAdminLogin.Trim());
                using var reader = await checkCmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    StatusMessage = $"Пользователь '{NewAdminLogin}' не найден";
                    return;
                }

                var userId = reader.GetInt32("id");
                await reader.CloseAsync();

                using var updateCmd = new MySqlCommand("UPDATE users SET is_admin = 1 WHERE id = @id", conn);
                updateCmd.Parameters.AddWithValue("@id", userId);
                var rows = await updateCmd.ExecuteNonQueryAsync();

                if (rows > 0)
                {
                    StatusMessage = $"Пользователь '{NewAdminLogin}' теперь администратор";
                    NewAdminLogin = string.Empty;
                    await LoadDataAsync();
                }
                else
                {
                    StatusMessage = "Ошибка при назначении админа";
                }
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            finally { _db.closeConnection(); }
        }

        [RelayCommand]
        private async Task ExportSongsAsync()
        {
            if (Songs.Count == 0)
            {
                StatusMessage = "Нет треков для экспорта";
                return;
            }

            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Экспорт треков WaveTune — {DateTime.Now:dd.MM.yyyy HH:mm}");
                sb.AppendLine(new string('-', 50));

                int i = 1;
                foreach (var s in Songs)
                {
                    sb.AppendLine($"{i++}. {s.Title} — {s.Author}");
                    if (s.Genre != null)
                        sb.AppendLine($"   Жанр: {s.Genre}");
                    if (s.AlbumId.HasValue)
                        sb.AppendLine($"   Альбом ID: {s.AlbumId}");
                }

                sb.AppendLine(new string('-', 50));
                sb.AppendLine($"Всего треков: {Songs.Count}");

                var fileName = $"songs_export_{DateTime.Now:yyyyMMdd_HHmm}.txt";
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                await File.WriteAllTextAsync(path, sb.ToString(), System.Text.Encoding.UTF8);

                StatusMessage = $"Экспорт сохранён: {fileName}";

                await (Application.Current?.MainPage?.DisplayAlert(
                    "Готово",
                    $"Файл сохранён:\n{path}",
                    "OK") ?? Task.CompletedTask);
            }
            catch (Exception ex) { StatusMessage = $"Ошибка экспорта: {ex.Message}"; }
        }
    }
}