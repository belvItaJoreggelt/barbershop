namespace barberShop
{
    public class Fodrasz
    {

        public int ID { get; set; }
        public string Specializacio { get; set; }
        public string Nev { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string? ProfilkepFajlNeve {get; set; }

        public ICollection<Szolgaltatas> VallaltSzolgaltatasok { get; set; } = new  List<Szolgaltatas>();
        public ICollection<FodraszMunkaIdo> FodraszMunkaidok { get; set; }
        public ICollection<FodraszSzunet> FodraszSzunetek { get; set; }
        public ICollection<Idopont> Idopontok { get; set; } = new List<Idopont>();  

        //public ICollection<SzabadSav> SzabadSavak { get; set; } = new List<SzabadSav>();
    }
}
