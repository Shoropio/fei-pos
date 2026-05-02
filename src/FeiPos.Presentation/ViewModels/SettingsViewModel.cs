using System;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FeiPos.Infrastructure.Services;
using Microsoft.Win32;

namespace FeiPos.Presentation.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ConfigurationService _configService;
        private readonly EscPosPrinterService _printerService;

        [ObservableProperty]
        private AppConfig _config;

        [ObservableProperty]
        private ObservableCollection<string> _availablePrinters = new();

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public SettingsViewModel(ConfigurationService configService, EscPosPrinterService printerService)
        {
            _configService = configService;
            _printerService = printerService;
            _config = _configService.Config;
            LoadPrinters();
        }

        private void LoadPrinters()
        {
            AvailablePrinters.Clear();
            foreach (string printer in PrinterSettings.InstalledPrinters)
                AvailablePrinters.Add(printer);
        }

        [RelayCommand]
        private void BrowseCertificate()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar Certificado Digital",
                Filter = "Certificado Digital (*.p12;*.pfx)|*.p12;*.pfx|Todos (*.*)|*.*",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() == true)
                Config.CertificatePath = dialog.FileName;
        }

        [RelayCommand]
        private void PrintTest()
        {
            try
            {
                _printerService.PrintTest();
                StatusMessage = $"Test enviado a: {Config.PrinterName}";
            } catch (Exception ex) {
                StatusMessage = $"Error al imprimir: {ex.Message}";
            }
        }

        [RelayCommand]
        private void RefreshPrinters()
        {
            LoadPrinters();
            StatusMessage = $"{AvailablePrinters.Count} impresoras encontradas.";
        }

        [RelayCommand]
        private void Save()
        {
            _configService.Save();
            StatusMessage = "Configuración guardada correctamente.";
        }
    }
}
