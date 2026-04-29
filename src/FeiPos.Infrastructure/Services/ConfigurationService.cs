using System;
using System.IO;
using System.Text.Json;

namespace FeiPos.Infrastructure.Services
{
    public class ConfigurationService
    {
        private const string ConfigFile = "appsettings.local.json";
        public AppConfig Config { get; private set; } = new();

        public ConfigurationService()
        {
            Load();
        }

        public void Load()
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            else
            {
                Config = new AppConfig();
                Save();
            }
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }
    }

    public class AppConfig
    {
        public string CompanyName { get; set; } = "Mi Comercio S.A.";
        public string TaxId { get; set; } = "3-101-123456";
        public string HaciendaApiUrl { get; set; } = "https://api.comprobanteselectronicos.go.cr/recepcion/v1/";
        public string HaciendaUser { get; set; } = "";
        public string CertificatePath { get; set; } = "C:\\Certs\\firma.p12";
        public string CertificatePin { get; set; } = "1234";
        public string TerminalId { get; set; } = "00001";
        public string OfficeId { get; set; } = "001";
    }
}
