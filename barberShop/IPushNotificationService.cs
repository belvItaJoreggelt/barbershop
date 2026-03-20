namespace barberShop
{
    public interface IPushNotificationService
    {
        Task SendBookingToBarberAsync(
        string barberExternalId,
        string barberName,
        string customerName,
        string serviceName,
        DateTime bookingTimeUtc);
    }
}
