namespace barberShop
{
    public class Szolgaltatas
    {

        public int Id {  get; set; }
        public string Nev { get; set; } = string.Empty;
        public int Idotartam { get; set; }
        public decimal Ar {  get; set; }
        public string Leiras {  get; set; }
        public string? KepFajlNeve { get; set; }
        public int Sorszam { get; set; }
    }
}
