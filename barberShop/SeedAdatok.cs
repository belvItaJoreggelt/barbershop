using Microsoft.EntityFrameworkCore;

namespace barberShop
{
    public class SeedAdatok
    {
        public static void Initialize(AppDbContext context)
        {
            if (!context.Fodraszok.Any())
            {
                var fodraszok = new Fodrasz[]
                {
                    new Fodrasz
                    {
                        Nev = "Kovács Titusz",
                        Email = "kovacstitusz@gmail.com",
                        Telefon = "+36 30 4533 345",
                        Specializacio = "Féfi hajvágás",
                        ProfilkepFajlNeve="kovacsTitusz.png"
                    },
                    new Fodrasz
                    {
                        Nev = "Német Tibériusz\r\n",
                        Email = "ntiberiusz@gmail.com\r\n",
                        Telefon = "+36 20 7879 454",
                        Specializacio = "Hajvágás",
                        ProfilkepFajlNeve="nemetTiberiusz.png"
                    }
                };
                context.Fodraszok.AddRange(fodraszok);
                context.SaveChanges();
            }

            if (!context.Szolgaltatasok.Any())
            {
                var szolgaltatasok = new Szolgaltatas[]
                {
                    new Szolgaltatas
                    {
                        Nev = "Férfi hajvágás",
                        Ar = 7000,
                        Idotartam = 45,
                        Leiras = "Legyen szó klasszikus fazonról vagy modern átmenetről, nálunk a precizitás az alap. A hajvágást alapos hajmosással és egyénre szabott stylinggal zárjuk, hogy egész nap magabiztos maradj.",
                        KepFajlNeve="hajvagasFekvo.png"
                    },
                    new Szolgaltatas
                    {
                        Nev = "Szakáll igazítás",
                        Ar = 3000,
                        Idotartam = 15,
                        Leiras = "Az ápolt szakáll meghatározza az arc karakterét, nálunk pedig minden a részletekről szól. A precíz formázást prémium " +
                                            "ápolók használatával tesszük teljessé, hogy megjelenésed markáns, stílusod pedig kifogástalan legyen.",
                        KepFajlNeve="szakallFekvo.png"
                    },
                    new Szolgaltatas
                    {
                        Nev = "Hajvágás + szakáll igazítás",
                        Ar = 9500,
                        Idotartam = 60,
                        Leiras = "Hozd összhangba a megjelenésed egyetlen látogatással, ahol a karakteres hajvágás és a tűpontos szakállformázás találkozik.",
                        KepFajlNeve="hajEsSzakallFekvo.png"
                    },
                    new Szolgaltatas
                    {
                        Nev = "Kreatív hajvágás",
                        Ar = 8000,
                        Idotartam = 60,
                        Leiras = "Extrém skin fade, precíz hajtetoválás vagy teljesen egyedi, aszimmetrikus formára vágysz? Nálunk valóra válnak a legmerészebb elképzeléseidet is!",
                        KepFajlNeve="kreativFekvo.png"
                    }
                };
                context.Szolgaltatasok.AddRange(szolgaltatasok);
                context.SaveChanges();
            }

            // Kapcsolatok: mindig ellenőrizzük, és ha üresek, feltöltjük
            var fodraszokLista = context.Fodraszok.Include(f => f.VallaltSzolgaltatasok).ToList();
            var szolgaltatasokLista = context.Szolgaltatasok.ToList();

            if (fodraszokLista.Count >= 2 && szolgaltatasokLista.Count >= 3)
            {
                bool needSave = false;

                if (!fodraszokLista[0].VallaltSzolgaltatasok.Any())
                {
                    fodraszokLista[0].VallaltSzolgaltatasok.Add(szolgaltatasokLista[0]); // Férfi hajvágás
                    fodraszokLista[0].VallaltSzolgaltatasok.Add(szolgaltatasokLista[1]); // Szakáll igazítás
                    needSave = true;
                }

                if (!fodraszokLista[1].VallaltSzolgaltatasok.Any())
                {
                    fodraszokLista[1].VallaltSzolgaltatasok.Add(szolgaltatasokLista[0]);
                    needSave = true;
                }

                if (needSave)
                    context.SaveChanges();
            }
        }
    }
}