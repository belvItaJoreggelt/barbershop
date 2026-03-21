namespace barberShop
{
    public class OneSignalOptions
    {
        public string AppId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        /// <summary>Safari (macOS/iOS) web push-hez kell. OneSignal Dashboard → Web Configuration.</summary>
        public string? SafariWebId { get; set; }
    }
}
