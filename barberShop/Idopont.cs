namespace barberShop
{
    public class Idopont
    {

        public int ID { get; set; }

        public int FodraszId { get; set; }
        public Fodrasz Fodrasz { get; set; } = null!;

        public int SzolgaltatasId {  get; set; }
        public Szolgaltatas Szolgaltatas { get; set; } = null!;

        public DateTime EsedekessegiIdopont {  get; set; }
        public DateTime FoglalasiIdopont { get; set; } = DateTime.UtcNow;

        //Vasarló adatok
        public string CustomerNeve { get; set; } = string.Empty;
        public string CustomerEmail {  get; set; } = string.Empty;
        public string CustomerPhone {  get; set; } = string.Empty;
        public string? CustomerNotes {  get; set; }

        

    }
}
