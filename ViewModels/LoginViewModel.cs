using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySqlConnector;
using System.Threading.Tasks;

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
            const string query = "SELECT password FROM users WHERE login = @login";

            using (var connection = db.getConnection())
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@login", Login);

                db.openConnection();
                var result = await command.ExecuteScalarAsync();
                db.closeConnection();

                if (result is null)
                {
                    ErrorMessage = "uncorrect login or password";
                    return;
                }

                var passwordFromDb = result.ToString() ?? string.Empty;

                if (passwordFromDb == Password)
                {
                    await Shell.Current.GoToAsync("//HomePage");
                }
                else
                {
                    ErrorMessage = "Неверный логин или пароль";
                }
            }
        }

        [RelayCommand]
        public async Task GoToRegisterPage()
        {
            await Shell.Current.GoToAsync("//RegisterPage");
        }
    }
}
