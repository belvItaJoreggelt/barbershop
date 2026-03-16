using Microsoft.AspNetCore.Identity;
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
                        ProfilkepFajlNeve = "kovacsTitusz.png"
                    },
                    new Fodrasz
                    {
                        Nev = "Német Tibériusz",
                        Email = "ntiberiusz@gmail.com",
                        Telefon = "+36 20 7879 454",
                        Specializacio = "Hajvágás",
                        ProfilkepFajlNeve = "nemetTiberiusz.png"
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
                        KepFajlNeve = "hajvagasFekvo.png",
                        KepFajlNev_Vilagos= "hajvagasFekvo_Vilagos"
                    },
                    new Szolgaltatas
                    {
                        Nev = "Szakáll igazítás",
                        Ar = 3000,
                        Idotartam = 15,
                        Leiras = "Az ápolt szakáll meghatározza az arc karakterét, nálunk pedig minden a részletekről szól. A precíz formázást prémium ápolók használatával tesszük teljessé, hogy megjelenésed markáns, stílusod pedig kifogástalan legyen.",
                        KepFajlNeve = "szakallFekvo.png",
                        KepFajlNev_Vilagos= "szakallFekv_Vilagos"
                    },
                    new Szolgaltatas
                    {
                        Nev = "Hajvágás + szakáll igazítás",
                        Ar = 9500,
                        Idotartam = 60,
                        Leiras = "Hozd összhangba a megjelenésed egyetlen látogatással, ahol a karakteres hajvágás és a tűpontos szakállformázás találkozik.",
                        KepFajlNeve = "hajEsSzakallFekvo.png",
                        KepFajlNev_Vilagos= "hajEsSzakallFekvo_Vilagos"
                    },
                    new Szolgaltatas
                    {
                        Nev = "Kreatív hajvágás",
                        Ar = 8000,
                        Idotartam = 60,
                        Leiras = "Extrém skin fade, precíz hajtetoválás vagy teljesen egyedi, aszimmetrikus formára vágysz? Nálunk valóra válnak a legmerészebb elképzeléseidet is!",
                        KepFajlNeve = "kreativFekvo.png",
                        KepFajlNev_Vilagos= "kreativFekvo_Vilagos"
                    }
                };
                context.Szolgaltatasok.AddRange(szolgaltatasok);
                context.SaveChanges();
            }

            var fodraszokLista = context.Fodraszok.Include(f => f.VallaltSzolgaltatasok).ToList();
            var szolgaltatasokLista = context.Szolgaltatasok.ToList();

            if (fodraszokLista.Count >= 2 && szolgaltatasokLista.Count >= 3)
            {
                bool needSave = false;

                if (!fodraszokLista[0].VallaltSzolgaltatasok.Any())
                {
                    fodraszokLista[0].VallaltSzolgaltatasok.Add(szolgaltatasokLista[0]);
                    fodraszokLista[0].VallaltSzolgaltatasok.Add(szolgaltatasokLista[1]);
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

        public static async Task SeedFodraszBejelentkezoekAsync(
            AppDbContext context,
            UserManager<Felhasznalo> userManager,
            string fodraszAlapJelszo = "fodrasz1!")
        {
            const string fodraszRole = "Fodrasz";
            var fodraszok = context.Fodraszok.ToList();

            foreach (var fodrasz in fodraszok)
            {
                var email = fodrasz.Email.Trim();
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new Felhasznalo
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        FodraszId = fodrasz.ID,
                        Nev = fodrasz.Nev
                    };
                    var result = await userManager.CreateAsync(user, fodraszAlapJelszo);
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(user, fodraszRole);
                }
            }
        }
    }
}