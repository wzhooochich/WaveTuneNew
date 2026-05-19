using CommunityToolkit.Mvvm.ComponentModel;
using MySqlConnector;
using WaveTuneNew.Models;
using WaveTuneNew.Services;

namespace WaveTuneNew.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly DataBase _db = new DataBase();

        [ObservableProperty]
        private string nickname = string.Empty;

        [ObservableProperty]
        private string avatarUrl = "default_avatar.png";

        [ObservableProperty]
        private string bio = string.Empty;

        [ObservableProperty]
        private string login = string.Empty;

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

            using var connection = _db.getConnection();
            await connection.OpenAsync();

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@userId", user.Id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                Nickname = reader["nickname"] as string ?? user.Login;
                AvatarUrl = reader["avatar_url"] as string ?? "default_avatar.png";
                Bio = reader["bio"] as string ?? string.Empty;
            }
            else
            {
                Nickname = user.Login;
            }
        }
    }
}