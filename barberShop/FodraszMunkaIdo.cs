namespace barberShop
{
    public class FodraszMunkaIdo
    {
        public int ID { get; set; }
        public int FodraszId { get; set; }
        public Fodrasz Fodrasz { get; set; } = null;
        public DateTime Datum { get; set; }
        public TimeSpan Kezdoido { get; set; }
        public TimeSpan ZaroIdo { get; set; }
    }
}
