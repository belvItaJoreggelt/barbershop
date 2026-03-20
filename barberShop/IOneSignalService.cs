namespace barberShop
{
    public interface IOneSignalService
    {
        Task SendPushToBarberAsync(int fodraszId, string title, string message);
    }
}
