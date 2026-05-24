using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySqlConnector;
using WaveTuneNew.Models;
using WaveTuneNew.Services;

namespace WaveTuneNew.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        private string login = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [RelayCommand]
        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Enter login and password";
                return;
            }

            var db = new DataBase();
            const string query = "SELECT id, password FROM users WHERE login = @login";

            using var connection = db.getConnection();
            await connection.OpenAsync();

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@login", Login);

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                ErrorMessage = "Неверный логин или пароль";
                return;
            }

            var passwordFromDb = reader["password"].ToString() ?? string.Empty;
            var userId = reader.GetInt32("id");

            if (passwordFromDb != Password)
            {
                ErrorMessage = "Неверный логин или пароль";
                return;
            }

            SessionService.CurrentUser = new User
            {
                Id = userId,
                Login = Login
            };

            await Shell.Current.GoToAsync("//HomePage");
        }

        [RelayCommand]
        public async Task GoToRegisterPage()
        {
            await Shell.Current.GoToAsync("//RegisterPage");
        }
    }
}