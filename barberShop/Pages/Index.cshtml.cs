using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace barberShop.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IEmailKuldo _emailKuldo;

        public IndexModel(AppDbContext context, IEmailKuldo emailKuldo)
        {
            _context = context;
            _emailKuldo = emailKuldo;
        }


        [BindProperty(SupportsGet = true)]
        public string? Section { get; set; } = "szolgaltatasok";
        public List<Szolgaltatas> Szolgaltatasok { get; set; } = new();


        [BindProperty(SupportsGet =true)]
        public int? SzolgaltatasId { get; set; }

        [BindProperty(SupportsGet =true)]
        public int? FodraszId { get; set; }
        public Szolgaltatas KivalasztottSzolgaltatas { get; set; } = null;
        public List<Fodrasz> Fodraszok { get; set; } = new List<Fodrasz>();
        public Dictionary<int, List<DateTime>> FodraszLegkorabbiSzabadIdopontok { get; set; } = new();


        //idpont kivalassz

        [BindProperty(SupportsGet =true)]
        public string? NaptarDatum { get; set; }


        public Fodrasz? KivalasztottF { get; set; }
        public List<DateTime> ReggeliIdopontok { get; set; } = new();
        public List<DateTime> DelutaniIdopontok { get; set; } = new();
        public List<DateTime> EstiIdopontok { get; set; } = new();



        //idopont foglal
        [BindProperty(SupportsGet = true)]
        public string? FoglalasDatum { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FoglalasIdo { get; set; }

        [BindProperty]
        [Required(ErrorMessage ="A név megadása kötelező")]
        public string UgyfelNev { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage ="Az e-mail cím megadása kötelező")]
        public string UgyfelEmail { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage ="Telefonszám megadása kötelező")]
        public string UgyfelTelefon { get; set; } = "";

        [BindProperty]
        public string? UgyfelMegjegyzes { get; set; }

        public async Task OnGetAsync()
        {
            Szolgaltatasok = await _context.Szolgaltatasok.OrderBy(s => s.Sorszam).ThenBy(n => n.Nev).ToListAsync();

            if (!SzolgaltatasId.HasValue)
                return;

            KivalasztottSzolgaltatas = await _context.Szolgaltatasok.FindAsync(SzolgaltatasId.Value);
            
            if (KivalasztottSzolgaltatas == null)
                return;

            Fodraszok = await _context.Fodraszok
                    .Where(f => f.VallaltSzolgaltatasok.Any(sz => sz.Id == SzolgaltatasId.Value))
                    .Include(f => f.VallaltSzolgaltatasok)
                    .Include(f => f.Idopontok)
                    .ThenInclude(f => f.Szolgaltatas)
                    .Include(f => f.FodraszMunkaidok)
                    .ToListAsync();

            if (FodraszId.HasValue)
                KivalasztottF = Fodraszok.FirstOrDefault(f => f.ID == FodraszId);

            // slot számítás BENT, a KivalasztottSzolgaltatas != null blokkban
            var szolgIdotartamPerc = KivalasztottSzolgaltatas.Idotartam;
            var most = DateTime.Now;

            foreach (var fodrasz in Fodraszok)
            {
                var szabadok = new List<DateTime>();
                for (int d = 0; d < 14; ++d)
                {
                    var datum = DateTime.Today.AddDays(d);
                    var datumUtc = DBDataTimeHelper.ToUtcDate(datum);
                    var munkaido = fodrasz.FodraszMunkaidok?.FirstOrDefault(m => m.Datum.Date == datumUtc.Date);
                    if (munkaido == null) continue;

                    var kezdo = munkaido.Kezdoido;
                    var zaro = munkaido.ZaroIdo;
                    var slotVegPerc = szolgIdotartamPerc;

                    while (kezdo.Add(TimeSpan.FromMinutes(slotVegPerc)) <= zaro)
                    {
                        var slotDt = datum.Date + kezdo;
                        if (slotDt >= most)
                        {
                            var slotVeg = slotDt.AddMinutes(szolgIdotartamPerc);
                            var utkozik = fodrasz.Idopontok?.Any(i =>
                            {
                                var iVeg = i.EsedekessegiIdopont.AddMinutes(i.Szolgaltatas?.Idotartam ?? 30);
                                return i.EsedekessegiIdopont < slotVeg && iVeg > slotDt;
                            }) ?? false;

                            if (!utkozik)
                            {
                                szabadok.Add(slotDt);
                                if (szabadok.Count >= 3) break;
                            }
                        }
                        kezdo = kezdo.Add(TimeSpan.FromMinutes(15));
                    }
                    if (szabadok.Count >= 3) break;
                }
                FodraszLegkorabbiSzabadIdopontok[fodrasz.ID] = szabadok;

                
                
            }//foreach vge

            if (Section=="osszesIdopont" && KivalasztottF !=null)
            {
                ReggeliIdopontok.Clear();
                DelutaniIdopontok.Clear();
                EstiIdopontok.Clear();

                var nap = DateTime.Today;
                if(!string.IsNullOrWhiteSpace(NaptarDatum) && DateTime.TryParse(NaptarDatum, out var parsedDate ))
                    nap=parsedDate.Date;

                NaptarDatum = nap.ToString("yyyy-MM-dd");

                var napUtc = DBDataTimeHelper.ToUtcDate(nap);
                var munkaido = await _context.FodraszMunkaidok
                    .FirstOrDefaultAsync(m => m.FodraszId == KivalasztottF.ID && m.Datum == napUtc);

                if (munkaido == null)
                    return;

                var kezdo2 = munkaido.Kezdoido;
                var zaro2 = munkaido.ZaroIdo;
                var hosszPerc = KivalasztottSzolgaltatas.Idotartam;

                var foglaltak = await _context.Idopontok
                    .Include(i => i.Szolgaltatas)
                    .Where(i => i.FodraszId == KivalasztottF.ID && i.EsedekessegiIdopont.Date == napUtc.Date)
                    .ToListAsync();

                while (kezdo2.Add(TimeSpan.FromMinutes(hosszPerc)) <= zaro2)
                {
                    var slot = nap.Date + kezdo2;
                    if (slot < most) { kezdo2 = kezdo2.Add(TimeSpan.FromMinutes(15)); continue; }
                    var veg = slot.AddMinutes(hosszPerc);

                    bool utkozik = foglaltak.Any(i =>
                    {
                        var iVeg = i.EsedekessegiIdopont.AddMinutes(i.Szolgaltatas.Idotartam);
                        return i.EsedekessegiIdopont < veg && iVeg > slot;
                    });

                    if (!utkozik)
                    {
                        if (slot.Hour < 12)
                            ReggeliIdopontok.Add(slot);
                        else if (slot.Hour < 17)
                            DelutaniIdopontok.Add(slot);
                        else
                            EstiIdopontok.Add(slot);
                    }

                    kezdo2 = kezdo2.Add(TimeSpan.FromMinutes(15));
                }
            }
        }


        public async Task<IActionResult> OnPostFoglalasAsync()
        {
            Section = "foglalas";

            if (!ModelState.IsValid)
            {
                if (SzolgaltatasId.HasValue)
                    KivalasztottSzolgaltatas = await _context.Szolgaltatasok.FindAsync(SzolgaltatasId);
                if (FodraszId.HasValue)
                    KivalasztottF = await _context.Fodraszok.FindAsync(FodraszId.Value);

                return Page();
            }

            if (!SzolgaltatasId.HasValue ||!FodraszId.HasValue || string.IsNullOrWhiteSpace(FoglalasDatum) || string.IsNullOrWhiteSpace(FoglalasIdo))
            {
                ModelState.AddModelError(string.Empty, "Hiányzó foglalaási adatok!");
                return Page();
            }

            if (!DateTime.TryParseExact($"{FoglalasDatum} {FoglalasIdo}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var kezdes))
            {
                ModelState.AddModelError(string.Empty, "Hibás adatok! 407");
                return Page();
            }

            var szolg = await _context.Szolgaltatasok.FindAsync(SzolgaltatasId.Value);
            var fodr = await _context.Fodraszok.FindAsync(FodraszId.Value);

            if (szolg == null ||fodr==null)
            {
                ModelState.AddModelError(string.Empty, "Hibás adatok! 409");
                return Page();
            }

            var idopont = new Idopont
            {
                FodraszId = fodr.ID,
                SzolgaltatasId = szolg.Id,
                EsedekessegiIdopont = DBDataTimeHelper.ToUtc(kezdes),
                FoglalasiIdopont = DBDataTimeHelper.ToUtc(DateTime.Now),
                CustomerNeve = UgyfelNev,
                CustomerEmail=UgyfelEmail,
                CustomerPhone=UgyfelTelefon,
                CustomerNotes= UgyfelMegjegyzes
            };

            _context.Idopontok.Add(idopont);
            await _context.SaveChangesAsync();


            // e-mail
            var subject = "BestBarberShop - foglalás visszaigazolása";
            var body = $@"Kedves {UgyfelNev}!

Foglalásodat sikeresen rögzítettük.

Várunk szerettel a lentebb feltüntetett időpontra!

Fodrász: {fodr.Nev}
Szolgáltatás: {szolg.Nev}
Időpont: {kezdes:yyyy.MM.dd HH:mm}

Tengermély tisztelettel:
BestBarberShop csapata";

            await _emailKuldo.SendAsync(UgyfelEmail, subject, body);

            return RedirectToPage("/Index", new {section= "koszi" });
        }
        //eddig és ne tovább
    }

}
