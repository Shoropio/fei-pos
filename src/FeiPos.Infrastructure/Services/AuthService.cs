using System;
using System.Linq;
using FeiPos.Domain.Entities;
using FeiPos.Infrastructure.Persistence;

namespace FeiPos.Infrastructure.Services
{
    public class AuthService
    {
        private const string DefaultAdminPassword = "admin123";
        private readonly AppDbContext _context;

        public AppUser? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;
        public bool IsAdmin => string.Equals(CurrentUser?.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        public bool MustChangePassword => CurrentUser?.MustChangePassword == true;
        public string CurrentUserName => CurrentUser?.FullName ?? CurrentUser?.UserName ?? "Sin sesion";

        public AuthService(AppDbContext context)
        {
            _context = context;
            EnsureBootstrapUsers();
            CurrentUser = _context.AppUsers.FirstOrDefault(u => u.IsCurrent && u.IsActive);
        }

        public bool Login(string userName, string password, out string message)
        {
            var normalizedUserName = (userName ?? string.Empty).Trim();
            var user = _context.AppUsers.FirstOrDefault(u => u.UserName == normalizedUserName);

            if (user == null || !user.IsActive)
            {
                message = "Usuario no existe o esta inactivo.";
                return false;
            }

            if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
            {
                message = "Usuario o contrasena incorrectos.";
                return false;
            }

            foreach (var appUser in _context.AppUsers)
            {
                appUser.IsCurrent = appUser.Id == user.Id;
            }

            user.LastLoginAt = DateTime.UtcNow;
            _context.SaveChanges();
            CurrentUser = user;
            message = $"Sesion iniciada: {user.FullName}";
            return true;
        }

        public void Logout()
        {
            foreach (var appUser in _context.AppUsers)
            {
                appUser.IsCurrent = false;
            }

            _context.SaveChanges();
            CurrentUser = null;
        }

        public AppUser CreateUser(string userName, string fullName, string role, string password)
        {
            var normalizedUserName = userName.Trim();
            if (_context.AppUsers.Any(u => u.UserName == normalizedUserName))
            {
                throw new InvalidOperationException("Ya existe un usuario con ese nombre.");
            }

            var (hash, salt) = PasswordHasher.HashPassword(password);
            var user = new AppUser
            {
                UserName = normalizedUserName,
                FullName = string.IsNullOrWhiteSpace(fullName) ? normalizedUserName : fullName.Trim(),
                Role = NormalizeRole(role),
                PasswordHash = hash,
                PasswordSalt = salt,
                IsActive = true,
                MustChangePassword = true
            };

            _context.AppUsers.Add(user);
            _context.SaveChanges();
            return user;
        }

        public void UpdateUser(AppUser user, string password)
        {
            var normalizedUserName = user.UserName.Trim();
            if (_context.AppUsers.Any(u => u.Id != user.Id && u.UserName == normalizedUserName))
            {
                throw new InvalidOperationException("Ya existe otro usuario con ese nombre.");
            }

            user.Role = NormalizeRole(user.Role);
            user.UserName = normalizedUserName;
            user.FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName.Trim();

            if (!string.IsNullOrWhiteSpace(password))
            {
                SetPassword(user, password);
            }

            _context.SaveChanges();
            if (CurrentUser?.Id == user.Id)
            {
                CurrentUser = user;
            }
        }

        public void ResetPassword(AppUser user, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Digite una contrasena nueva.");
            }

            SetPassword(user, password);
            user.MustChangePassword = true;
            _context.SaveChanges();
        }

        public bool ChangeCurrentPassword(string currentPassword, string newPassword, string confirmPassword, out string message)
        {
            if (CurrentUser == null)
            {
                message = "No hay sesion activa.";
                return false;
            }

            if (!PasswordHasher.Verify(currentPassword, CurrentUser.PasswordHash, CurrentUser.PasswordSalt))
            {
                message = "La contrasena actual no coincide.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            {
                message = "La nueva contrasena debe tener al menos 4 caracteres.";
                return false;
            }

            if (newPassword != confirmPassword)
            {
                message = "La confirmacion no coincide.";
                return false;
            }

            SetPassword(CurrentUser, newPassword);
            CurrentUser.MustChangePassword = false;
            _context.SaveChanges();
            message = "Contrasena actualizada.";
            return true;
        }

        private void EnsureBootstrapUsers()
        {
            var users = _context.AppUsers.ToList();
            if (!users.Any())
            {
                CreateUser("admin", "Administrador", "Admin", DefaultAdminPassword);
                var admin = _context.AppUsers.First(u => u.UserName == "admin");
                admin.IsCurrent = false;
                admin.MustChangePassword = true;
                _context.SaveChanges();
                return;
            }

            foreach (var user in users.Where(u => string.IsNullOrWhiteSpace(u.PasswordHash) || string.IsNullOrWhiteSpace(u.PasswordSalt)))
            {
                SetPassword(user, string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? DefaultAdminPassword : "1234");
                user.MustChangePassword = true;
            }

            _context.SaveChanges();
        }

        private static void SetPassword(AppUser user, string password)
        {
            var (hash, salt) = PasswordHasher.HashPassword(password);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
        }

        private static string NormalizeRole(string role)
        {
            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Cashier";
        }
    }
}
