using Microsoft.AspNetCore.Identity;
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

        public IndexModel(
            AppDbContext context,
            UserManager<Felhasznalo> userManager,
            IEmailKuldo emailKuldo,
            IPushNotificationService pushNotificationService)
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

        public Szolgaltatas? KivalasztottSzolgaltatas { get; set; }
        public List<Fodrasz> Fodraszok { get; set; } = new();
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
            Szolgaltatasok = await _context.Szolgaltatasok
                .OrderBy(s => s.Sorszam)
                .ThenBy(n => n.Nev)
                .ToListAsync();

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
                .Where(f => f.VallaltSzolgaltatasok.Any(sz => sz.Id == SzolgaltatasId.Value))
                .Include(f => f.VallaltSzolgaltatasok)
                .Include(f => f.Idopontok)
                    .ThenInclude(i => i.Szolgaltatas)
                .Include(f => f.FodraszMunkaidok)
                .ToListAsync();

            if (FodraszId.HasValue)
                KivalasztottF = Fodraszok.FirstOrDefault(f => f.ID == FodraszId.Value);
        }

        private void SzamoldLegkorabbiSzabadSlotokat()
        {
            var szolgIdotartamPerc = KivalasztottSzolgaltatas!.Idotartam;
            var mostBudapest = BudapestTime.UtcToBudapest(DateTime.UtcNow);

            foreach (var fodrasz in Fodraszok)
            {
                var szabadok = new List<DateTime>();

                for (int d = 0; d < 14; ++d)
                {
                    var datumBudapest = BudapestTime.TodayBudapestDate.AddDays(d);

                    var napKezdetBudapest = DateTime.SpecifyKind(datumBudapest.Date, DateTimeKind.Unspecified);
                    var kovetkezoNapKezdetBudapest = napKezdetBudapest.AddDays(1);

                    DateTime napKezdetUtc;
                    DateTime kovetkezoNapKezdetUtc;

                    try
                    {
                        napKezdetUtc = BudapestTime.BudapestLocalToUtc(napKezdetBudapest);
                        kovetkezoNapKezdetUtc = BudapestTime.BudapestLocalToUtc(kovetkezoNapKezdetBudapest);
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }

                    var munkaido = fodrasz.FodraszMunkaidok?
                        .FirstOrDefault(m =>
                            m.Datum >= napKezdetUtc &&
                            m.Datum < kovetkezoNapKezdetUtc);

                    if (munkaido == null)
                        continue;

                    var kezdo = munkaido.Kezdoido;
                    var zaro = munkaido.ZaroIdo;

                    while (kezdo.Add(TimeSpan.FromMinutes(szolgIdotartamPerc)) <= zaro)
                    {
                        var slotBudapest = datumBudapest.Date + kezdo;

                        if (slotBudapest >= mostBudapest)
                        {
                            var slotVegBudapest = slotBudapest.AddMinutes(szolgIdotartamPerc);

                            var utkozik = fodrasz.Idopontok?.Any(i =>
                            {
                                var foglaltKezdesBudapest = BudapestTime.UtcToBudapest(i.EsedekessegiIdopont);
                                var foglaltVegBudapest = foglaltKezdesBudapest.AddMinutes(i.Szolgaltatas?.Idotartam ?? 30);

                                return foglaltKezdesBudapest < slotVegBudapest
                                       && foglaltVegBudapest > slotBudapest;
                            }) ?? false;

                            if (!utkozik)
                            {
                                szabadok.Add(slotBudapest);

                                if (szabadok.Count >= 3)
                                    break;
                            }
                        }

                        kezdo = kezdo.Add(TimeSpan.FromMinutes(15));
                    }

                    if (szabadok.Count >= 3)
                        break;
                }

                FodraszLegkorabbiSzabadIdopontok[fodrasz.ID] = szabadok;
            }
        }

        private async Task LoadNapraSzabadIdopontokAsync()
        {
            ReggeliIdopontok.Clear();
            DelutaniIdopontok.Clear();
            EstiIdopontok.Clear();

            var napBudapest = BudapestTime.TodayBudapestDate;

            if (!string.IsNullOrWhiteSpace(NaptarDatum) &&
                DateTime.TryParseExact(
                    NaptarDatum,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate))
            {
                napBudapest = parsedDate.Date;
            }

            NaptarDatum = napBudapest.ToString("yyyy-MM-dd");

            var napKezdetBudapest = DateTime.SpecifyKind(napBudapest.Date, DateTimeKind.Unspecified);
            var kovetkezoNapKezdetBudapest = napKezdetBudapest.AddDays(1);

            DateTime napKezdetUtc;
            DateTime kovetkezoNapKezdetUtc;

            try
            {
                napKezdetUtc = BudapestTime.BudapestLocalToUtc(napKezdetBudapest);
                kovetkezoNapKezdetUtc = BudapestTime.BudapestLocalToUtc(kovetkezoNapKezdetBudapest);
            }
            catch (ArgumentException)
            {
                return;
            }

            var munkaido = await _context.FodraszMunkaidok
                .FirstOrDefaultAsync(m =>
                    m.FodraszId == KivalasztottF!.ID &&
                    m.Datum >= napKezdetUtc &&
                    m.Datum < kovetkezoNapKezdetUtc);

            if (munkaido == null)
                return;

            var kezdo = munkaido.Kezdoido;
            var zaro = munkaido.ZaroIdo;
            var hosszPerc = KivalasztottSzolgaltatas!.Idotartam;

            var foglaltak = await _context.Idopontok
                .Include(i => i.Szolgaltatas)
                .Where(i =>
                    i.FodraszId == KivalasztottF!.ID &&
                    i.EsedekessegiIdopont >= napKezdetUtc &&
                    i.EsedekessegiIdopont < kovetkezoNapKezdetUtc)
                .ToListAsync();

            var mostBudapest = BudapestTime.UtcToBudapest(DateTime.UtcNow);

            while (kezdo.Add(TimeSpan.FromMinutes(hosszPerc)) <= zaro)
            {
                var slotBudapest = napBudapest.Date + kezdo;

                if (slotBudapest < mostBudapest)
                {
                    kezdo = kezdo.Add(TimeSpan.FromMinutes(15));
                    continue;
                }

                var slotVegBudapest = slotBudapest.AddMinutes(hosszPerc);

                DateTime slotUtc;
                DateTime slotVegUtc;

                try
                {
                    slotUtc = BudapestTime.BudapestLocalToUtc(
                        DateTime.SpecifyKind(slotBudapest, DateTimeKind.Unspecified));

                    slotVegUtc = BudapestTime.BudapestLocalToUtc(
                        DateTime.SpecifyKind(slotVegBudapest, DateTimeKind.Unspecified));
                }
                catch (ArgumentException)
                {
                    kezdo = kezdo.Add(TimeSpan.FromMinutes(15));
                    continue;
                }

                bool utkozik = foglaltak.Any(i =>
                {
                    var foglaltVegUtc = i.EsedekessegiIdopont.AddMinutes(i.Szolgaltatas.Idotartam);
                    return i.EsedekessegiIdopont < slotVegUtc && foglaltVegUtc > slotUtc;
                });

                if (!utkozik)
                {
                    if (slotBudapest.Hour < 12)
                        ReggeliIdopontok.Add(slotBudapest);
                    else if (slotBudapest.Hour < 17)
                        DelutaniIdopontok.Add(slotBudapest);
                    else
                        EstiIdopontok.Add(slotBudapest);
                }

                kezdo = kezdo.Add(TimeSpan.FromMinutes(15));
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
                    KivalasztottSzolgaltatas = await _context.Szolgaltatasok.FindAsync(SzolgaltatasId.Value);

                if (FodraszId.HasValue)
                    KivalasztottF = await _context.Fodraszok.FindAsync(FodraszId.Value);

                return Page();
            }

            if (!SzolgaltatasId.HasValue ||
                !FodraszId.HasValue ||
                string.IsNullOrWhiteSpace(FoglalasDatum) ||
                string.IsNullOrWhiteSpace(FoglalasIdo))
            {
                ModelState.AddModelError(string.Empty, "Hiányzó foglalási adatok!");
                return Page();
            }

            if (!DateTime.TryParseExact(
                    $"{FoglalasDatum} {FoglalasIdo}",
                    "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var kezdesBudapest))
            {
                ModelState.AddModelError(string.Empty, "Hibás adatok! 407");
                return Page();
            }

            DateTime kezdesUtc;
            try
            {
                kezdesUtc = BudapestTime.BudapestLocalToUtc(
                    DateTime.SpecifyKind(kezdesBudapest, DateTimeKind.Unspecified));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                if (SzolgaltatasId.HasValue)
                    KivalasztottSzolgaltatas = await _context.Szolgaltatasok.FindAsync(SzolgaltatasId.Value);

                if (FodraszId.HasValue)
                    KivalasztottF = await _context.Fodraszok.FindAsync(FodraszId.Value);

                return Page();
            }

            var szolg = await _context.Szolgaltatasok.FindAsync(SzolgaltatasId.Value);
            var fodr = await _context.Fodraszok.FindAsync(FodraszId.Value);

            if (szolg == null || fodr == null)
            {
                ModelState.AddModelError(string.Empty, "Hibás adatok! 409");
                return Page();
            }

            var foglalasVegeUtc = kezdesUtc.AddMinutes(szolg.Idotartam);

            bool utkozik = await _context.Idopontok
                .Include(i => i.Szolgaltatas)
                .AnyAsync(i =>
                    i.FodraszId == fodr.ID &&
                    i.EsedekessegiIdopont < foglalasVegeUtc &&
                    i.EsedekessegiIdopont.AddMinutes(i.Szolgaltatas.Idotartam) > kezdesUtc);

            if (utkozik)
            {
                ModelState.AddModelError(string.Empty, "Ez az időpont időközben már foglalt lett.");

                KivalasztottSzolgaltatas = szolg;
                KivalasztottF = fodr;
                return Page();
            }

            var idopont = new Idopont
            {
                FodraszId = fodr.ID,
                SzolgaltatasId = szolg.Id,
                EsedekessegiIdopont = kezdesUtc,
                FoglalasiIdopont = DateTime.UtcNow,
                CustomerNeve = UgyfelNev,
                CustomerEmail = UgyfelEmail,
                CustomerPhone = UgyfelTelefon,
                CustomerNotes = UgyfelMegjegyzes
            };

            if (await _context.Idopontok.AnyAsync(i=>i.EsedekessegiIdopont == kezdesUtc && i.FodraszId == fodr.ID))
            {
                ModelState.AddModelError(string.Empty, "Erre az időpontra már van foglalás.");
                KivalasztottSzolgaltatas = szolg;
                KivalasztottF = fodr;
                return Page();
            }

            _context.Idopontok.Add(idopont);
            await _context.SaveChangesAsync();

            var subject = "BestBarberShop - foglalás visszaigazolása";

            var hu = CultureInfo.GetCultureInfo("hu-HU");
            var idopontSzoveg = kezdesBudapest.ToString("MMMM d. (dddd) HH:mm", hu);
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
        h2 {{ color: rgba(191, 162, 122, 0.7); }}
    </style>
</head>
<body>
<div style=""text-align: center;"">
    <h2>Kedves {nevH}!</h2>
    <p>Köszönjük, hogy minket választottál!</p>
    <p>Foglalásod részletei:</p>
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""margin: 0 auto; max-width: 320px;"">
        <tr>
            <td style=""padding: 10px 18px; text-align: center; border-radius: 15px; background-color: #e8dcc8; background-image: linear-gradient(to top right, rgba(191, 162, 122, 0.7) 0%, rgb(252, 251, 249) 59%, rgb(255, 255, 255) 100%); border: solid 0.5px #eceae6;"">
                {szolgNevH}<br />
                {idopontSzoveg}<br />
                {arSzoveg} Ft
            </td>
        </tr>
    </table>
    <p style=""padding-top: 20px;"">
        BestBarbershop<br />
        <a href=""{maps}"" style=""color: black;"">1115 Budapest Bártfai utca 38</a><br />
        <a href=""mailto:szaszakpepe@gmail.com"" style=""text-decoration: none; color: black;"">szaszakpepe@gmail.com</a><br />
        <a href=""tel:+36307271232"" style=""text-decoration: none; color: black;"">+36 30 727 1232</a>
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