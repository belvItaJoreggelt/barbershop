namespace barberShop
{
    public class FodraszReferenciaFoto
    {
        public int Id { get; set; }
        public int FodraszId { get; set; }
        public Fodrasz Fodrasz { get; set; } = null;
        public string Fajlnev { get; set; } = "";
        public DateTime FeltoltveUtc { get; set; } = DateTime.UtcNow;
        public string Sorrend { get; set; } = "";
    }
}
