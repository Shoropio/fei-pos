using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeiPos.Infrastructure.Services;

namespace FeiPos.Presentation.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _authService;

        [ObservableProperty] private string _userName = "admin";
        [ObservableProperty] private string _statusMessage = "Usuario inicial: admin / admin123";
        [ObservableProperty] private bool _isPasswordChangeRequired;

        public bool LoginSucceeded { get; private set; }

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
        }

        [RelayCommand]
        private void Login(string password)
        {
            if (_authService.Login(UserName, password, out var message))
            {
                if (_authService.MustChangePassword)
                {
                    IsPasswordChangeRequired = true;
                    StatusMessage = "Debe cambiar la contrasena antes de continuar.";
                    return;
                }

                LoginSucceeded = true;
            }

            StatusMessage = message;
        }

        public bool ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (_authService.ChangeCurrentPassword(currentPassword, newPassword, confirmPassword, out var message))
            {
                LoginSucceeded = true;
                StatusMessage = message;
                return true;
            }

            StatusMessage = message;
            return false;
        }
    }
}
