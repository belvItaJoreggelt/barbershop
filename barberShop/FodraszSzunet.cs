namespace barberShop
{
    public class FodraszSzunet
    {
        public int ID { get; set; }
        public int FodraszId { get; set; }
        public Fodrasz Fodrasz { get; set; } = null;
        public DateTime Datum { get; set; }
        public TimeSpan KezdoIdo { get; set; }
        public TimeSpan ZaroIdo { get; set; }
    }
}
