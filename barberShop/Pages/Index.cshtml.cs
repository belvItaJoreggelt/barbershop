using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;

namespace barberShop.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Felhasznalo> _userManager;
        private readonly IEmailKuldo _emailKuldo;
        private readonly IPushNotificationService _pushNotificationService;
        public IndexModel(AppDbContext context,UserManager<Felhasznalo> userManager, IEmailKuldo emailKuldo, IPushNotificationService pushNotificationService)
        {
            _context = context;
            _userManager = userManager;
            _emailKuldo = emailKuldo;
            _pushNotificationService = pushNotificationService;
        }


        [BindProperty(SupportsGet = true)]
        public string? Section { get; set; } = "szolgaltatasok";
        public List<Szolgaltatas> Szolgaltatasok { get; set; } = new();


        [BindProperty(SupportsGet = true)]
        public int? SzolgaltatasId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FodraszId { get; set; }
        public Szolgaltatas KivalasztottSzolgaltatas { get; set; } = null;
        public List<Fodrasz> Fodraszok { get; set; } = new List<Fodrasz>();
        public Dictionary<int, List<DateTime>> FodraszLegkorabbiSzabadIdopontok { get; set; } = new();


        [BindProperty(SupportsGet = true)]
        public string? NaptarDatum { get; set; }


        public Fodrasz? KivalasztottF { get; set; }
        public List<DateTime> ReggeliIdopontok { get; set; } = new();
        public List<DateTime> DelutaniIdopontok { get; set; } = new();
        public List<DateTime> EstiIdopontok { get; set; } = new();


        [BindProperty(SupportsGet = true)]
        public string? FoglalasDatum { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FoglalasIdo { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "A név megadása kötelező")]
        public string UgyfelNev { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Az e-mail cím megadása kötelező")]
        public string UgyfelEmail { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Telefonszám megadása kötelező")]
        public string UgyfelTelefon { get; set; } = "";

        [BindProperty]
        public string? UgyfelMegjegyzes { get; set; }

        [BindProperty]
        public bool AdatKezHozzaJ { get; set; }



        public async Task OnGetAsync()
        {
            Szolgaltatasok = await _context.Szolgaltatasok.OrderBy(s => s.Sorszam).ThenBy(n => n.Nev).ToListAsync();

            if (!SzolgaltatasId.HasValue)
                return;
            await LoadSzolgaltatasEsFodraszokAsync();

            if (KivalasztottSzolgaltatas == null)
                return;
            SzamoldLegkorabbiSzabadSlotokat();

            if (Section == "osszesIdopont" && KivalasztottF != null)
                await LoadNapraSzabadIdopontokAsync();

            if (Section == "foglalas")
                await FelhasznAdatokBetolt();
        }

        private async Task LoadSzolgaltatasEsFodraszokAsync()
        {
            KivalasztottSzolgaltatas = await _context.Szolgaltatasok.FindAsync(SzolgaltatasId!.Value);
            if (KivalasztottSzolgaltatas == null)
                return;

            Fodraszok = await _context.Fodraszok
                .Where(f => f.VallaltSzolgaltatasok.Any(sz => sz.Id == SzolgaltatasId!.Value))
                .Include(f => f.VallaltSzolgaltatasok)
                .Include(f => f.Idopontok)
                .ThenInclude(f => f.Szolgaltatas)
                .Include(f => f.FodraszMunkaidok)
                .ToListAsync();

            if (FodraszId.HasValue)
                KivalasztottF = Fodraszok.FirstOrDefault(f => f.ID == FodraszId);
        }

        private void SzamoldLegkorabbiSzabadSlotokat()
        {
            var szolgIdotartamPerc = KivalasztottSzolgaltatas!.Idotartam;
            var most = DateTime.Now;

            foreach (var fodrasz in Fodraszok)
            {
                var szabadok = new List<DateTime>();
                for (int d = 0; d < 14; ++d)
                {
                    var datum = DateTime.Today.AddDays(d);
                    var munkaido = fodrasz.FodraszMunkaidok?.FirstOrDefault(m => m.Datum.Date == datum.Date);
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
            }
        }

        private async Task LoadNapraSzabadIdopontokAsync()
        {
            ReggeliIdopontok.Clear();
            DelutaniIdopontok.Clear();
            EstiIdopontok.Clear();

            var nap = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(NaptarDatum) && DateTime.TryParse(NaptarDatum, out var parsedDate))
                nap = parsedDate.Date;

            NaptarDatum = nap.ToString("yyyy-MM-dd");
            var napUtc = DBDataTimeHelper.ToUtcDate(nap);

            var munkaido = await _context.FodraszMunkaidok
                .FirstOrDefaultAsync(m => m.FodraszId == KivalasztottF!.ID && m.Datum.Date == napUtc.Date);

            if (munkaido == null)
                return;

            var kezdo2 = munkaido.Kezdoido;
            var zaro2 = munkaido.ZaroIdo;
            var hosszPerc = KivalasztottSzolgaltatas!.Idotartam;

            var foglaltak = await _context.Idopontok
                .Include(i => i.Szolgaltatas)
                .Where(i => i.FodraszId == KivalasztottF!.ID && i.EsedekessegiIdopont.Date == napUtc.Date)
                .ToListAsync();

            var most = DateTime.Now;

            while (kezdo2.Add(TimeSpan.FromMinutes(hosszPerc)) <= zaro2)
            {
                var slot = nap.Date + kezdo2;
                if (slot < most)
                {
                    kezdo2 = kezdo2.Add(TimeSpan.FromMinutes(15));
                    continue;
                }
                var veg = slot.AddMinutes(hosszPerc);

                var slotUtc = DBDataTimeHelper.ToUtc(slot);
                var vegUtc = DBDataTimeHelper.ToUtc(veg);

                bool utkozik = foglaltak.Any(i =>
                {
                    var iVeg = i.EsedekessegiIdopont.AddMinutes(i.Szolgaltatas.Idotartam);
                    return i.EsedekessegiIdopont < vegUtc && iVeg > slotUtc;
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


        public async Task<IActionResult> OnPostFoglalasAsync()
        {
            Section = "foglalas";

            if (!AdatKezHozzaJ)
            {
                ModelState.AddModelError("AdatKezHozzaJ", "Az adatkezeléshez való hozzájárulás kötelező.");
            }

            if (!ModelState.IsValid)
            {
                if (SzolgaltatasId.HasValue)
                    KivalasztottSzolgaltatas = await _context.Szolgaltatasok.FindAsync(SzolgaltatasId);
                if (FodraszId.HasValue)
                    KivalasztottF = await _context.Fodraszok.FindAsync(FodraszId.Value);

                return Page();
            }

            if (!SzolgaltatasId.HasValue || !FodraszId.HasValue || string.IsNullOrWhiteSpace(FoglalasDatum) || string.IsNullOrWhiteSpace(FoglalasIdo))
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

            if (szolg == null || fodr == null)
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
                CustomerEmail = UgyfelEmail,
                CustomerPhone = UgyfelTelefon,
                CustomerNotes = UgyfelMegjegyzes
            };

            _context.Idopontok.Add(idopont);
            await _context.SaveChangesAsync();


            var subject = "BestBarberShop - foglalás visszaigazolása";


            var hu = CultureInfo.GetCultureInfo("hu-HU");
            var idopontSzoveg = kezdes.ToString("MMMM d. (dddd) HH:mm", hu);
            var arSzoveg = szolg.Ar.ToString("N0", hu);
            var nevH = WebUtility.HtmlEncode(UgyfelNev);
            var szolgNevH = WebUtility.HtmlEncode(szolg.Nev);

            var maps = "https://www.bing.com/maps/search?mepi=72%7ELocal%7EEmbedded%7EEntity_Vertical_List_Card&ty=17&poicount=18&sei=0&FORM=MPSRPL&q=kelenf%C3%B6ld+fodr%C3%A1szat&secq=%C3%9Ajhull%C3%A1m+Fodr%C3%A1szat+kelenfoeld+fodraszat&sece=ypid.YN8081x11846474530400285953&ppois=47.467506408691406_19.035743713378906_%C3%9Ajhull%C3%A1m+Fodr%C3%A1szat_YN8081x11846474530400285953%7E47.46304702758789_19.034894943237305_X%C3%A9nia+Fodr%C3%A1szat_YN8081x14308692530027957564%7E47.46721649169922_19.042898178100586_B%C3%A1rtfai+Sz%C3%A9ps%C3%A9gszalon+most_YN8081x3342422111653719704%7E&segment=Local&cp=47.467179%7E19.036090&lvl=17.7&style=r";


            var body = $@"
<html lang=""hu"">
<head>
    <meta charset=""utf-8"" />
    <style type=""text/css"">
        body {{ font-family: Arial, Helvetica, sans-serif; color: #333; }}
        h2{{color: rgba(191, 162, 122, 0.7);}}
    </style>
</head>
<body>
<div style=""text-align: center;"">
    <h2>Kedves {nevH}!</h2>
    <p>Köszönjük, hogy minket választottál!</p>
    <p>Foglalásod részletei:</p>
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""margin: 0 auto; max-width: 320px;"">
        <tr>
            <td style=""padding: 10px 18px; text-align: center; border-radius: 15px; background-color: #e8dcc8; background-image: linear-gradient(to top right, rgba(191,162,122,0.7), rgb(218, 213, 213)); border: solid 0.5px rgb(218, 213, 213);"">
                {szolgNevH}<br />
                {idopontSzoveg}<br />
                {arSzoveg}Ft
            </td>
        </tr>
    </table>
    <p style=""padding-top: 20px;"">
        BestBarbershop<br />
        <a href=""{maps}"">1115 Budapest Bártfai utca 38</a><br />
        <a href=""mailto:szaszakpepe@gmail.com"">szaszakpepe@gmail.com</a><br />
        <a href=""tel:+36307271232"">+36 30 727 1232</a>
    </p>
</div>
</body>
</html>";

            await _emailKuldo.SendAsync(UgyfelEmail, subject, body);
            /*
           var barberExternalId = $"fodrasz-{fodr.ID}";

           try
           {
               await _pushNotificationService.SendBookingToBarberAsync(
                   barberExternalId,
                   fodr.Nev,
                   UgyfelNev,
                   szolg.Nev,
                   idopont.EsedekessegiIdopont
               );
           }
           catch
           {
               // itt később lehet logolni, de a foglalást ne akadályozza meg
           }
           */
            return RedirectToPage("/Index", new { section = "koszi" });
        }

        public async Task FelhasznAdatokBetolt()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    UgyfelEmail = user.Email ?? "";
                    UgyfelNev = user.Nev ?? "";
                    UgyfelTelefon = user.PhoneNumber ?? "";
                    
                }
            }
        }
    }
}