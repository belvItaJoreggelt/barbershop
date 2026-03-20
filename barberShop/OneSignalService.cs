using System.Net.Http.Json;

namespace barberShop
{
    public class OneSignalService : IOneSignalService
    {
        private readonly HttpClient _httpClient;
        private readonly OneSignalSettings _settings;

        public OneSignalService(HttpClient httpClient, Microsoft.Extensions.Options.IOptions<OneSignalSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task SendPushToBarberAsync(int fodraszId, string title, string message)
        {
            if (string.IsNullOrWhiteSpace(_settings.RestApiKey) || string.IsNullOrWhiteSpace(_settings.AppId))
                return;

            var payload = new
            {
                app_id = _settings.AppId,
                target_channel = "push",
                include_aliases = new { external_id = new[] { "fodrasz_" + fodraszId } },
                contents = new { en = message },
                headings = new { en = title }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.onesignal.com/notifications");
            request.Headers.Add("Authorization", "Key " + _settings.RestApiKey);
            request.Content = JsonContent.Create(payload);

            try
            {
                await _httpClient.SendAsync(request);
            }
            catch (Exception)
            {
                // Ne vesszen el a foglalás miatta – logolható később
            }
        }
    }
}
