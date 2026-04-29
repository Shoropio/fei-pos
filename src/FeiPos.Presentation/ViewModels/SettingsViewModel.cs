using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeiPos.Infrastructure.Services;

namespace FeiPos.Presentation.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ConfigurationService _configService;

        [ObservableProperty]
        private AppConfig _config;

        public SettingsViewModel(ConfigurationService configService)
        {
            _configService = configService;
            _config = _configService.Config;
        }

        [RelayCommand]
        private void Save()
        {
            _configService.Save();
            // Notificar éxito al usuario
        }
    }
}
