using System.Net.Http;
using System.Text;
using System.Text.Json;
using FeiPos.Application.Interfaces;
using FeiPos.Domain.Entities;

namespace FeiPos.Infrastructure.Services
{
    public partial class HaciendaService : IHaciendaService
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigurationService _configService;

        public HaciendaService(HttpClient httpClient, ConfigurationService configService)
        {
            _httpClient = httpClient;
            _configService = configService;
        }


        public async Task<string> SignXml(string xmlRaw, string certificatePath, string pin)
        {
            // Usar los parámetros o la configuración por defecto
            var path = string.IsNullOrEmpty(certificatePath) ? _configService.Config.CertificatePath : certificatePath;
            var certPin = string.IsNullOrEmpty(pin) ? _configService.Config.CertificatePin : pin;

            return await Task.Run(() => Security.XmlDigitalSigner.SignXades(xmlRaw, path, certPin));
        }

        public Task<bool> SendToHacienda(string signedXml, string token)
        {
            // POST al endpoint de Hacienda
            // endpoint: https://api.comprobanteselectronicos.go.cr/recepcion/v1/recepcion
            return Task.FromResult(true);
        }

        public Task<ElectronicInvoiceStatus> CheckStatus(string key)
        {
            // GET al endpoint de Hacienda para consultar estado por Clave
            return Task.FromResult(ElectronicInvoiceStatus.Accepted);
        }
    }
}
