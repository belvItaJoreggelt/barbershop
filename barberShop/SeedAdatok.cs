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
                        Specializacio = "Féfi hajvágás"
                    },
                    new Fodrasz
                    {
                        Nev = "Kis Tímea",
                        Email = "kistim@gmail.com",
                        Telefon = "+36 20 7879 454",
                        Specializacio = "Női hajvágás"
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
                        Leiras = "Legyen szó klasszikus úriember stílusról vagy modern átmenetről, nálunk a precizitás az alap. Szolgáltatásunk során profi hajvágóeszközökkel " +
                            "a kívánt formára igazítjuk a hajadat, ezt követően pedig alapos öblítéssel kimossuk a frissen levágott hajszálakat. A folyamatot egy egyénre szabott " +
                            "hajformázással (styling) zárjuk, hogy ne csak a székből felállva, hanem egész nap magabiztosnak érezd magad."
                    },
                    new Szolgaltatas
                    {
                        Nev = "Szakáll igazítás",
                        Ar = 3000,
                        Idotartam = 15,
                        Leiras = "Egy igényesen formázott szakáll sokat hozzáad a megjelenéshez, ezért nálunk minden igazítás a részletekről szól. A kezelés során a szakáll hosszát és formáját az arc karakteréhez igazítjuk, " +
                            "éles, rendezett vonalakat alakítunk ki az arcon és a nyakon, majd eltávolítjuk a felesleges szőrszálakat. A szolgáltatás végén ápoló és formázó termékekkel tesszük teljessé az eredményt, " +
                            "hogy szakállad frissen igazított, rendezett és tartósan stílusos maradjon a nap folyamán."
                    },
                    new Szolgaltatas
                    {
                        Nev = "Férfi hajvágás + szakáll igazítás",
                        Ar = 9500,
                        Idotartam = 60,
                        Leiras = "Komplett frissítés egy alkalom alatt: a hajvágás és a szakáll igazítása együtt biztosít rendezett, harmonikus megjelenést. A frizurát a kívánt stílus szerint alakítjuk ki, majd " +
                            "szakálladat az arcformádhoz igazítva formázzuk, tiszta kontúrokat és egységes összképet kialakítva. A szolgáltatást hajformázással és szakállápoló termékek alkalmazásával zárjuk, " +
                            "hogy ne csak frissen vágva, hanem egész nap ápolt és magabiztos megjelenéssel távozz."
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