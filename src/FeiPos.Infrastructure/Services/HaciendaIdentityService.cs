using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FeiPos.Infrastructure.Services
{
    public class HaciendaIdentityService
    {
        private readonly HttpClient _httpClient;
        private const string IdpUrl = "https://idp.comprobanteselectronicos.go.cr/auth/realms/rut/protocol/openid-connect/token";

        public HaciendaIdentityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> GetAccessTokenAsync(string username, string password, string clientId = "api-prod")
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", clientId),
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
