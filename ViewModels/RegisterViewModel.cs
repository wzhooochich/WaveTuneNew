using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySqlConnector;
using WaveTuneNew.Models;
using WaveTuneNew.Services;

namespace WaveTuneNew.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        [ObservableProperty]
        private string login = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [RelayCommand]
        private async Task RegisterAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Enter login and password";
                return;
            }

            var db = new DataBase();
            const string checkQuery = "SELECT COUNT(*) FROM users WHERE login = @login";
            const string insertQuery = "INSERT INTO users (login, password) VALUES (@login, @password)";
            const string getIdQuery = "SELECT id FROM users WHERE login = @login";

            using var connection = db.getConnection();
            await connection.OpenAsync();

            using (var checkCmd = new MySqlCommand(checkQuery, connection))
            {
                checkCmd.Parameters.AddWithValue("@login", Login);
                var count = (long)(await checkCmd.ExecuteScalarAsync())!;
                if (count > 0)
                {
                    ErrorMessage = "This login is already taken";
                    return;
                }
            }

            using (var insertCmd = new MySqlCommand(insertQuery, connection))
            {
                insertCmd.Parameters.AddWithValue("@login", Login);
                insertCmd.Parameters.AddWithValue("@password", Password);
                var rows = await insertCmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    ErrorMessage = "Failed to create account";
                    return;
                }
            }

            int newUserId;
            using (var getIdCmd = new MySqlCommand(getIdQuery, connection))
            {
                getIdCmd.Parameters.AddWithValue("@login", Login);
                newUserId = Convert.ToInt32(await getIdCmd.ExecuteScalarAsync());
            }

            SessionService.CurrentUser = new User
            {
                Id = newUserId,
                Login = Login
            };

            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}