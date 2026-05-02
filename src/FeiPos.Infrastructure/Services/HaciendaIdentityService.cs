using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FeiPos.Infrastructure.Services;

namespace FeiPos.Infrastructure.Services
{
    public class HaciendaIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigurationService _configService;

        private const string IdpUrlProd    = "https://idp.comprobanteselectronicos.go.cr/auth/realms/rut/protocol/openid-connect/token";
        private const string IdpUrlSandbox = "https://idp.comprobanteselectronicos.go.cr/auth/realms/rut-stag/protocol/openid-connect/token";

        private string IdpUrl => _configService.Config.UseSandbox ? IdpUrlSandbox : IdpUrlProd;
        private string ClientId => _configService.Config.UseSandbox ? "api-stag" : "api-prod";

        public HaciendaIdentityService(HttpClient httpClient, ConfigurationService configService)
        {
            _httpClient = httpClient;
            _configService = configService;
        }

        /// <summary>
        /// Obtiene un token de acceso usando las credenciales guardadas en la configuración.
        /// Cambia automáticamente entre producción y sandbox según Config.UseSandbox.
        /// </summary>
        public async Task<string?> GetAccessTokenAsync()
        {
            return await GetAccessTokenAsync(
                _configService.Config.ApiUsername,
                _configService.Config.ApiPassword);
        }

        public async Task<string?> GetAccessTokenAsync(string username, string password)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("scope", "")
            });

            var response = await _httpClient.PostAsync(IdpUrl, content);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString();
        }
    }
}
