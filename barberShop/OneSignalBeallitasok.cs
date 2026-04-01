using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace barberShop
{
    public class OneSignalBeallitasok
    {
        /// <summary>OneSignal Dashboard → Settings → Keys & IDs → OneSignal App ID</summary>
        public string AppId { get; set; } = "";

        /// <summary>REST API Key (szerver oldali küldéshez – ne add ki a kliensnek)</summary>
        public string RestApiKey { get; set; } = "";

        /// <summary>
        /// Azure App Service: <c>OneSignal__ApiKey</c> → <c>ApiKey</c> (ugyanaz, mint RestApiKey).
        /// </summary>
        public string ApiKey
        {
            get => RestApiKey;
            set => RestApiKey = value ?? "";
        }

        /// <summary>Web Push – Safari (opcionális)</summary>
        public string SafariWebId { get; set; } = "";

        /// <summary>Fejlesztéshez: http://localhost engedélyezése</summary>
        public bool AllowLocalhostAsSecureOrigin { get; set; }
    }

    public interface IPushNotificationService
    {
        /// <summary>
        /// Push a megadott External User ID-hoz (pl. fodrasz-5).
        /// Ha nincs beállítva AppId/RestApiKey, csendben kihagyja.
        /// </summary>
        Task SendBookingToBarberAsync(
            string barberExternalId,
            string customerName,
            string serviceName,
            DateTime appointmentUtc,
            CancellationToken cancellationToken = default);
    }

    public sealed class OneSignalPushNotificationService : IPushNotificationService
    {
        private readonly HttpClient _http;
        private readonly OneSignalBeallitasok _opts;
        private readonly ILogger<OneSignalPushNotificationService> _logger;

        public OneSignalPushNotificationService(
            HttpClient http,
            IOptions<OneSignalBeallitasok> options,
            ILogger<OneSignalPushNotificationService> logger)
        {
            _http = http;
            _opts = options.Value;
            _logger = logger;
        }

        public async Task SendBookingToBarberAsync(
            string barberExternalId,
            string customerName,
            string serviceName,
            DateTime appointmentUtc,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_opts.AppId) || string.IsNullOrWhiteSpace(_opts.RestApiKey))
            {
                _logger.LogDebug("OneSignal push kihagyva: üres AppId vagy RestApiKey.");
                return;
            }

            if (string.IsNullOrWhiteSpace(barberExternalId))
                return;

            var idopontHu = BudapestTime.UtcToBudapest(appointmentUtc).ToString("yyyy.MM.dd HH:mm");

            var body = $"{customerName} · {serviceName} · {idopontHu}";

            var payload = new OneSignalCreateNotificationRequest
            {
                AppId = _opts.AppId,
                IncludeExternalUserIds = new List<string> { barberExternalId },
                Headings = new Dictionary<string, string> { ["hu"] = "Új foglalás", ["en"] = "New booking" },
                Contents = new Dictionary<string, string> { ["hu"] = body, ["en"] = body }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.onesignal.com/notifications");
            request.Headers.TryAddWithoutValidation("Authorization", $"Key {_opts.RestApiKey}");
            request.Content = JsonContent.Create(payload, options: new System.Text.Json.JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            try
            {
                var response = await _http.SendAsync(request, cancellationToken);
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("OneSignal API válasz: {Status} {Body}", response.StatusCode, responseText);
                else
                    _logger.LogInformation("OneSignal push elküldve: {ExternalId}", barberExternalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OneSignal push küldési hiba.");
            }
        }
    }

#pragma warning disable CA2227 // JSON DTO
    internal sealed class OneSignalCreateNotificationRequest
    {
        [JsonPropertyName("app_id")]
        public string AppId { get; set; } = "";

        [JsonPropertyName("include_external_user_ids")]
        public List<string>? IncludeExternalUserIds { get; set; }

        [JsonPropertyName("headings")]
        public Dictionary<string, string>? Headings { get; set; }

        [JsonPropertyName("contents")]
        public Dictionary<string, string>? Contents { get; set; }
    }
#pragma warning restore CA2227
}
