using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using OxmlWord = DocumentFormat.OpenXml.Wordprocessing;
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

        private async Task LoadDataAsync() // ПОТОКИ — async/await
        {
            IsLoading = true;
            try // ОБРАБОТКА ИСКЛЮЧЕНИЙ
            {
                _db.openConnection();
                var conn = _db.getConnection();

                // БД (взаимосвязанные таблицы) — запрос к таблице songs
                // СОРТИРОВКА — ORDER BY title
                using var cmdS = new MySqlCommand("SELECT id, title, author, picture_url, file_path, genre, album_id, user_id FROM songs ORDER BY title", conn);
                using var rS = await cmdS.ExecuteReaderAsync();
                var tempSongs = new ObservableCollection<Song>();
                while (await rS.ReadAsync())
                    tempSongs.Add(new Song { Id = rS.GetInt32("id"), Title = rS.GetString("title"), Author = rS.GetString("author"), PictureUrl = rS["picture_url"] as string ?? "damage.png", FilePath = rS["file_path"] as string ?? "", Genre = Song.ParseGenre(rS["genre"] as string), AlbumId = rS["album_id"] as int?, UserId = rS["user_id"] as int? });
                Songs = tempSongs;
                await rS.CloseAsync();

                // БД — запрос к таблице users (разграничение прав: WHERE is_admin = 1)
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
        private async Task ExportSongsAsync() // ЭКСПОРТ В WORD
        {
            if (Songs.Count == 0)
            {
                StatusMessage = "Нет треков для экспорта";
                return;
            }

            try 
            {
                var fileName = $"songs_export_{DateTime.Now:yyyyMMdd_HHmm}.docx";
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                // ЭКСПОРТ В WORD — DocumentFormat.OpenXml
                using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new OxmlWord.Document(new OxmlWord.Body());
                var body = mainPart.Document.Body!;

                body.Append(new OxmlWord.Paragraph(
                    new OxmlWord.ParagraphProperties(new OxmlWord.Justification { Val = OxmlWord.JustificationValues.Center }),
                    new OxmlWord.Run(
                        new OxmlWord.RunProperties(new OxmlWord.Bold(), new OxmlWord.FontSize { Val = "32" }),
                        new OxmlWord.Text($"Треки WaveTune — {DateTime.Now:dd.MM.yyyy HH:mm}")
                    )
                ));
                body.Append(new OxmlWord.Paragraph(new OxmlWord.Run(new OxmlWord.Text(""))));

                int i = 1;
                foreach (var s in Songs)
                {
                    var para = new OxmlWord.Paragraph();
                    para.Append(new OxmlWord.Run(
                        new OxmlWord.RunProperties(new OxmlWord.Bold()),
                        new OxmlWord.Text($"{i++}. {s.Title}") { Space = SpaceProcessingModeValues.Preserve }
                    ));
                    para.Append(new OxmlWord.Run(
                        new OxmlWord.Text($" — {s.Author}") { Space = SpaceProcessingModeValues.Preserve }
                    ));
                    if (s.Genre != null)
                        para.Append(new OxmlWord.Run(
                            new OxmlWord.RunProperties(new OxmlWord.Color { Val = "888888" }),
                            new OxmlWord.Text($"  [{s.Genre}]") { Space = SpaceProcessingModeValues.Preserve }
                        ));
                    body.Append(para);
                }

                body.Append(new OxmlWord.Paragraph(new OxmlWord.Run(new OxmlWord.Text(""))));
                body.Append(new OxmlWord.Paragraph(
                    new OxmlWord.Run(
                        new OxmlWord.RunProperties(new OxmlWord.Italic()),
                        new OxmlWord.Text($"Всего треков: {Songs.Count}")
                    )
                ));

                mainPart.Document.Save();

                StatusMessage = $"Экспорт сохранён: {fileName}";
                await (Application.Current?.MainPage?.DisplayAlert("Готово", $"Файл сохранён:\n{path}", "OK") ?? Task.CompletedTask);
            }
            catch (Exception ex) { StatusMessage = $"Ошибка экспорта: {ex.Message}"; }
        }
    }
}