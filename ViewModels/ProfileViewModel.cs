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

        public string DisplayName => string.IsNullOrWhiteSpace(Nickname) ? Login : Nickname;

        partial void OnNicknameChanged(string value) => OnPropertyChanged(nameof(DisplayName));
        partial void OnLoginChanged(string value) => OnPropertyChanged(nameof(DisplayName));

        public ProfileViewModel()
        {
            _ = LoadProfileAsync();
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