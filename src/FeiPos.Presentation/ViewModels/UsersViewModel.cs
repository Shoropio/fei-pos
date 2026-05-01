using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeiPos.Domain.Entities;
using FeiPos.Infrastructure.Persistence;
using FeiPos.Infrastructure.Services;

namespace FeiPos.Presentation.ViewModels
{
    public partial class UsersViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;

        [ObservableProperty] private ObservableCollection<AppUser> _users = new();
        [ObservableProperty] private AppUser? _selectedUser;
        [ObservableProperty] private string _userName = string.Empty;
        [ObservableProperty] private string _fullName = string.Empty;
        [ObservableProperty] private string _role = "Cashier";
        [ObservableProperty] private string _password = string.Empty;
        [ObservableProperty] private string _currentUser = "Sin sesion";
        [ObservableProperty] private string _statusMessage = string.Empty;

        public string[] Roles { get; } = { "Cashier", "Admin" };

        public UsersViewModel(AppDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
            LoadUsers();
        }

        partial void OnSelectedUserChanged(AppUser? value)
        {
            if (value == null)
            {
                return;
            }

            UserName = value.UserName;
            FullName = value.FullName;
            Role = value.Role;
            Password = string.Empty;
        }

        [RelayCommand]
        private void LoadUsers()
        {
            Users = new ObservableCollection<AppUser>(_context.AppUsers.OrderBy(u => u.UserName).ToList());
            CurrentUser = _authService.CurrentUserName;
        }

        [RelayCommand]
        private void AddUser()
        {
            if (string.IsNullOrWhiteSpace(UserName))
            {
                StatusMessage = "Digite el usuario.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Digite una contrasena inicial.";
                return;
            }

            try
            {
                _authService.CreateUser(UserName, FullName, Role, Password);
                ClearForm();
                LoadUsers();
                StatusMessage = "Usuario creado";
            }
            catch (System.Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        [RelayCommand]
        private void SaveSelected()
        {
            if (SelectedUser == null)
            {
                StatusMessage = "Seleccione un usuario.";
                return;
            }

            SelectedUser.UserName = UserName;
            SelectedUser.FullName = FullName;
            SelectedUser.Role = Role;

            try
            {
                _authService.UpdateUser(SelectedUser, Password);
                Password = string.Empty;
                LoadUsers();
                StatusMessage = "Usuario actualizado";
            }
            catch (System.Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        [RelayCommand]
        private void ResetPassword()
        {
            if (SelectedUser == null)
            {
                StatusMessage = "Seleccione un usuario.";
                return;
            }

            try
            {
                _authService.ResetPassword(SelectedUser, Password);
                Password = string.Empty;
                StatusMessage = "Contrasena actualizada";
            }
            catch (System.Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        [RelayCommand]
        private void ToggleActive()
        {
            if (SelectedUser == null)
            {
                StatusMessage = "Seleccione un usuario.";
                return;
            }

            if (_authService.CurrentUser?.Id == SelectedUser.Id && SelectedUser.IsActive)
            {
                StatusMessage = "No puede desactivar el usuario activo.";
                return;
            }

            SelectedUser.IsActive = !SelectedUser.IsActive;
            _context.SaveChanges();
            LoadUsers();
            StatusMessage = SelectedUser.IsActive ? "Usuario activado" : "Usuario desactivado";
        }

        [RelayCommand]
        private void ClearForm()
        {
            SelectedUser = null;
            UserName = string.Empty;
            FullName = string.Empty;
            Role = "Cashier";
            Password = string.Empty;
        }
    }
}
