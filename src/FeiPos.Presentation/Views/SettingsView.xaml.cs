using System.IO;
using System.Windows;
using System.Windows.Controls;
using FeiPos.Infrastructure.Services;

namespace FeiPos.Presentation.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not FeiPos.Presentation.ViewModels.SettingsViewModel vm) return;

            // Cargar contraseñas desde config (sin exponerlas como texto en binding)
            ApiPasswordBox.Password = vm.Config.ApiPassword ?? string.Empty;
            CertPinBox.Password     = vm.Config.CertificatePin ?? string.Empty;

            // Actualizar estado del certificado
            UpdateCertStatus(vm.Config.CertificatePath);
        }

        private void ApiPasswordBox_Changed(object sender, RoutedEventArgs e)
        {
            if (DataContext is FeiPos.Presentation.ViewModels.SettingsViewModel vm)
                vm.Config.ApiPassword = ApiPasswordBox.Password;
        }

        private void CertPinBox_Changed(object sender, RoutedEventArgs e)
        {
            if (DataContext is FeiPos.Presentation.ViewModels.SettingsViewModel vm)
            {
                vm.Config.CertificatePin = CertPinBox.Password;
            }
        }

        private void UpdateCertStatus(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                CertStatusText.Text = "Sin certificado configurado.";
                CertStatusText.Foreground = System.Windows.Media.Brushes.Gray;
                return;
            }

            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                CertStatusText.Text = $"Certificado encontrado — {info.Name} ({info.Length / 1024} KB)";
                CertStatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
            } else {
                CertStatusText.Text = $"Archivo no encontrado: {path}";
                CertStatusText.Foreground = System.Windows.Media.Brushes.Salmon;
            }
        }
    }
}
