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

        // Hacienda ATV
        public string HaciendaApiUrl { get; set; } = "https://api.comprobanteselectronicos.go.cr/recepcion/v1/";
        public string HaciendaIdpUrl { get; set; } = "https://idp.comprobanteselectronicos.go.cr/auth/realms/rut/protocol/openid-connect/token";
        public bool UseSandbox { get; set; } = false;
        public string ApiUsername { get; set; } = "";
        public string ApiPassword { get; set; } = "";

        // Firma Digital
        public string CertificatePath { get; set; } = "";
        public string CertificatePin { get; set; } = "";

        // Local del negocio
        public string TerminalId { get; set; } = "00001";
        public string OfficeId { get; set; } = "001";
        public string EconomicActivity { get; set; } = "000000";
        public string Province { get; set; } = "1";
        public string Canton { get; set; } = "01";
        public string District { get; set; } = "01";
        public string Neighborhood { get; set; } = "01";
        public string Address { get; set; } = "Otras señas";
        public string Phone { get; set; } = "2222-3333";
        public string Email { get; set; } = "ventas@comercio.com";

        // Impresora
        public string PrinterName { get; set; } = "";
        public bool AutoPrintReceipt { get; set; } = true;
        public bool OpenDrawerOnSale { get; set; } = false;
    }
}
