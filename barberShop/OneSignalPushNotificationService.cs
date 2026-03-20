using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace barberShop
{
    public class OneSignalPushNotificationService : IPushNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly OneSignalOptions _options;

        public OneSignalPushNotificationService(
            HttpClient httpClient,
            IOptions<OneSignalOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task SendBookingToBarberAsync(
            string barberExternalId,
            string barberName,
            string customerName,
            string serviceName,
            DateTime bookingTimeUtc)
        {
            if (string.IsNullOrWhiteSpace(_options.AppId))
                throw new Exception("OneSignal AppId nincs beállítva.");

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new Exception("OneSignal ApiKey nincs beállítva.");

            if (string.IsNullOrWhiteSpace(barberExternalId))
                throw new Exception("A cél barberExternalId üres.");

            var bookingLocal = bookingTimeUtc.ToLocalTime();

            var payload = new
            {
                app_id = _options.AppId,
                include_aliases = new
                {
                    external_id = new[] { barberExternalId }
                },
                target_channel = "push",
                headings = new
                {
                    en = "New booking",
                    hu = "Új foglalás"
                },
                contents = new
                {
                    en = $"{customerName} booked {serviceName} for {bookingLocal:yyyy-MM-dd HH:mm}",
                    hu = $"Új foglalás érkezett. Vendég: {customerName}. Szolgáltatás: {serviceName}. Időpont: {bookingLocal:yyyy.MM.dd HH:mm}"
                },
                web_url = "/Account/FodraszFelulet",
                data = new
                {
                    type = "booking",
                    barberExternalId = barberExternalId,
                    barber = barberName,
                    customer = customerName,
                    service = serviceName,
                    when = bookingLocal.ToString("yyyy-MM-dd HH:mm")
                }
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.onesignal.com/notifications?c=push");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Key", _options.ApiKey);

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"OneSignal push küldés hiba. HTTP {(int)response.StatusCode}: {responseText}");
            }
        }
    }
}