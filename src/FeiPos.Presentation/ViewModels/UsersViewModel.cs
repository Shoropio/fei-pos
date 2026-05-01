using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeiPos.Domain.Entities;
using FeiPos.Infrastructure.Persistence;

namespace FeiPos.Presentation.ViewModels
{
    public partial class UsersViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty] private ObservableCollection<AppUser> _users = new();
        [ObservableProperty] private AppUser? _selectedUser;
        [ObservableProperty] private string _userName = string.Empty;
        [ObservableProperty] private string _fullName = string.Empty;
        [ObservableProperty] private string _role = "Cashier";
        [ObservableProperty] private string _currentUser = "Sin sesión";
        [ObservableProperty] private string _statusMessage = string.Empty;

        public UsersViewModel(AppDbContext context)
        {
            _context = context;
            EnsureAdminUser();
            LoadUsers();
        }

        [RelayCommand]
        private void LoadUsers()
        {
            Users = new ObservableCollection<AppUser>(_context.AppUsers.OrderBy(u => u.UserName).ToList());
            CurrentUser = Users.FirstOrDefault(u => u.IsCurrent)?.FullName ?? "Sin sesión";
        }

        [RelayCommand]
        private void AddUser()
        {
            if (string.IsNullOrWhiteSpace(UserName)) return;
            _context.AppUsers.Add(new AppUser
            {
                UserName = UserName.Trim(),
                FullName = string.IsNullOrWhiteSpace(FullName) ? UserName.Trim() : FullName.Trim(),
                Role = string.IsNullOrWhiteSpace(Role) ? "Cashier" : Role.Trim()
            });
            _context.SaveChanges();
            UserName = string.Empty;
            FullName = string.Empty;
            Role = "Cashier";
            LoadUsers();
        }

        [RelayCommand]
        private void SetCurrent()
        {
            if (SelectedUser == null) return;
            foreach (var user in _context.AppUsers)
            {
                user.IsCurrent = user.Id == SelectedUser.Id;
            }
            _context.SaveChanges();
            LoadUsers();
            StatusMessage = $"Sesión activa: {CurrentUser}";
        }

        [RelayCommand]
        private void Logout()
        {
            foreach (var user in _context.AppUsers)
            {
                user.IsCurrent = false;
            }
            _context.SaveChanges();
            LoadUsers();
            StatusMessage = "Sesión cerrada";
        }

        private void EnsureAdminUser()
        {
            if (_context.AppUsers.Any()) return;
            _context.AppUsers.Add(new AppUser
            {
                UserName = "admin",
                FullName = "Administrador",
                Role = "Admin",
                IsCurrent = true
            });
            _context.SaveChanges();
        }
    }
}
