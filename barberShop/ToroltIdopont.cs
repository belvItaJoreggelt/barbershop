namespace barberShop
{
    public class ToroltIdopont
    {
        public int Id { get; set; }

        public int? EredetiIdopontId { get; set; }

        public int FodraszId { get; set; }
        public Fodrasz Fodrasz { get; set; } = null!;

        public int SzolgaltatasId { get; set; }

        public DateTime EsedekessegiIdopont { get; set; }
        public DateTime FoglalasiIdopont { get; set; } = DateTime.UtcNow;

        //Vasarló adatok
        public string CustomerNeve { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerNotes { get; set; }

        public DateTime TorolveUtc { get; set; }

        public Szolgaltatas? Szolgaltatas { get; set; }
    }
}
