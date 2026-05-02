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

        private string ApiUrl => _configService.Config.UseSandbox
            ? "https://api.comprobanteselectronicos.go.cr/recepcion-sandbox/v1/"
            : "https://api.comprobanteselectronicos.go.cr/recepcion/v1/";

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

        public async Task<bool> SendToHacienda(string signedXml, string token)
        {
            var key = ExtractKeyFromXml(signedXml);
            var date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            
            var payload = new
            {
                clave = key,
                fecha = date,
                emisor = ExtractEmisorFromXml(signedXml),
                receptor = ExtractReceptorFromXml(signedXml),
                comprobanteXml = Convert.ToBase64String(Encoding.UTF8.GetBytes(signedXml))
            };

            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl + "recepcion")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<ElectronicInvoiceStatus> CheckStatus(string key)
        {
            // Nota: Para consultar el estado se requiere token, pero aquí simplificamos el flujo
            // En una implementación real, se debe obtener un token antes de llamar a este método
            // o pasarlo como parámetro.
            
            var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl + "recepcion/" + key);
            // request.Headers.Authorization = ...

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return ElectronicInvoiceStatus.PendingSend;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var statusStr = doc.RootElement.GetProperty("ind-estado").GetString();

            return statusStr switch
            {
                "aceptado" => ElectronicInvoiceStatus.Accepted,
                "rechazado" => ElectronicInvoiceStatus.Rejected,
                _ => ElectronicInvoiceStatus.Sent
            };
        }

        private object ExtractEmisorFromXml(string xml)
        {
            // Lógica para extraer datos del emisor del XML
            return new { numeroIdentificacion = _configService.Config.TaxId, tipoIdentificacion = "01" };
        }

        private object? ExtractReceptorFromXml(string xml)
        {
            // Lógica para extraer datos del receptor del XML
            return null; // Opcional para Factura Electrónica si es Tiquete
        }

        private string ExtractKeyFromXml(string xml)
        {
            // Lógica para extraer la clave de 50 dígitos
            return _configService.Config.TerminalId; // Placeholder
        }
    }
}
