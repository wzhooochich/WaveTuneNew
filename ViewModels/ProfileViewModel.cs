using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySqlConnector;
using WaveTuneNew.Models;
using WaveTuneNew.Services;

namespace WaveTuneNew.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        [ObservableProperty]
        private string nickname = string.Empty;

        [ObservableProperty]
        private string avatarUrl = "default_avatar.png";

        [ObservableProperty]
        private string bio = string.Empty;

        [ObservableProperty]
        private string login = string.Empty;

        [ObservableProperty]
        private bool isTracksTabVisible = true;

        [ObservableProperty]
        private bool isAlbumsTabVisible = false;

        [ObservableProperty]
        private Color tracksTabButtonColor = Color.FromHex("#602191");

        [ObservableProperty]
        private Color albumsTabButtonColor = Color.FromHex("#FF252526");

        public ObservableCollection<Song> LikedSongs { get; } = new();
        public ObservableCollection<Album> LikedAlbums { get; } = new();

        public string DisplayName => string.IsNullOrWhiteSpace(Nickname) ? Login : Nickname;

        partial void OnNicknameChanged(string value) => OnPropertyChanged(nameof(DisplayName));
        partial void OnLoginChanged(string value) => OnPropertyChanged(nameof(DisplayName));

        public ProfileViewModel()
        {
            _ = LoadProfileAndDataAsync();
        }

        private async Task LoadProfileAndDataAsync()
        {
            await LoadProfileAsync();
            await LoadLikedSongsAsync();
            await LoadLikedAlbumsAsync();
        }

        private async Task LoadProfileAsync()
        {
            var user = SessionService.CurrentUser;
            if (user == null) return;

            Login = user.Login;
            const string query = "SELECT nickname, avatar_url, bio FROM user_profiles WHERE user_id = @userId";

            var db = new DataBase();
            using var connection = db.getConnection();
            await connection.OpenAsync();

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@userId", user.Id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                Nickname = reader["nickname"] as string ?? string.Empty;
                AvatarUrl = reader["avatar_url"] as string ?? "default_avatar.png";
                Bio = reader["bio"] as string ?? string.Empty;
            }
        }

        private async Task LoadLikedSongsAsync()
        {
            var user = SessionService.CurrentUser;
            if (user == null) return;

            const string query = @"
                SELECT s.* FROM songs s
                JOIN user_liked_songs uls ON s.id = uls.song_id
                WHERE uls.user_id = @userId
                ORDER BY uls.liked_at DESC";

            try
            {
                var db = new DataBase();
                using var connection = db.getConnection();
                await connection.OpenAsync();

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@userId", user.Id);

                using var reader = await cmd.ExecuteReaderAsync();
                LikedSongs.Clear();

                while (await reader.ReadAsync())
                {
                    LikedSongs.Add(new Song
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Title = reader["title"]?.ToString() ?? "Неизвестный трек"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private async Task LoadLikedAlbumsAsync()
        {
            var user = SessionService.CurrentUser;
            if (user == null) return;

            const string query = @"
                SELECT a.* FROM albums a
                JOIN user_liked_albums ula ON a.id = ula.album_id
                WHERE ula.user_id = @userId
                ORDER BY ula.liked_at DESC";

            try
            {
                var db = new DataBase();
                using var connection = db.getConnection();
                await connection.OpenAsync();

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@userId", user.Id);

                using var reader = await cmd.ExecuteReaderAsync();
                LikedAlbums.Clear();

                while (await reader.ReadAsync())
                {
                    LikedAlbums.Add(new Album
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Title = reader["title"]?.ToString() ?? "Неизвестный альбом"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        [RelayCommand]
        private void SwitchToTracks()
        {
            IsTracksTabVisible = true;
            IsAlbumsTabVisible = false;
            TracksTabButtonColor = Color.FromHex("#602191");
            AlbumsTabButtonColor = Color.FromHex("#FF252526");
        }

        [RelayCommand]
        private void SwitchToAlbums()
        {
            IsTracksTabVisible = false;
            IsAlbumsTabVisible = true;
            TracksTabButtonColor = Color.FromHex("#FF252526");
            AlbumsTabButtonColor = Color.FromHex("#602191");
        }

        [RelayCommand]
        private async Task PickAvatarAsync()
        {
            var user = SessionService.CurrentUser;
            if (user == null) return;

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images
            });
            if (result == null) return;

            AvatarUrl = result.FullPath;

            const string checkQuery = "SELECT COUNT(*) FROM user_profiles WHERE user_id = @userId";
            const string insertQuery = "INSERT INTO user_profiles (user_id, nickname, avatar_url, bio) VALUES (@userId, @nickname, @avatarUrl, @bio)";
            const string updateQuery = "UPDATE user_profiles SET avatar_url = @avatarUrl WHERE user_id = @userId";

            var db = new DataBase();
            using var connection = db.getConnection();
            await connection.OpenAsync();

            long count;
            using (var checkCmd = new MySqlCommand(checkQuery, connection))
            {
                checkCmd.Parameters.AddWithValue("@userId", user.Id);
                count = (long)(await checkCmd.ExecuteScalarAsync())!;
            }

            if (count == 0)
            {
                using var insertCmd = new MySqlCommand(insertQuery, connection);
                insertCmd.Parameters.AddWithValue("@userId", user.Id);
                insertCmd.Parameters.AddWithValue("@nickname", Nickname);
                insertCmd.Parameters.AddWithValue("@avatarUrl", AvatarUrl);
                insertCmd.Parameters.AddWithValue("@bio", Bio);
                await insertCmd.ExecuteNonQueryAsync();
            }
            else
            {
                using var updateCmd = new MySqlCommand(updateQuery, connection);
                updateCmd.Parameters.AddWithValue("@avatarUrl", AvatarUrl);
                updateCmd.Parameters.AddWithValue("@userId", user.Id);
                await updateCmd.ExecuteNonQueryAsync();
            }
        }
    }
}