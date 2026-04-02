using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;

namespace barberShop.Pages.Account
{
    [Authorize(Roles = "Fodrasz")]
    public class FodraszFeluletModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Felhasznalo> _userManager;
        private readonly IEmailKuldo _emailKuldo;

        public FodraszFeluletModel(
            AppDbContext context,
            UserManager<Felhasznalo> userManager,
            IEmailKuldo emailKuldo)
        {
            _context = context;
            _userManager = userManager;
            _emailKuldo = emailKuldo;
        }

        [BindProperty(SupportsGet = true)]
        public string Section { get; set; } = "";

        #region Adataim
        public Fodrasz? FodraszProfil { get; set; }
        public List<Szolgaltatas> OsszesSzolgaltatas { get; set; } = new();

        [BindProperty]
        public string EditNev { get; set; } = "";

        [BindProperty]
        public string EditEmail { get; set; } = "";

        [BindProperty]
        public string EditTelefon { get; set; } = "";

        [BindProperty]
        public string EditSpecializacio { get; set; } = "";

        [BindProperty]
        public string? UjJelszo { get; set; }

        [BindProperty]
        public List<int>? SelectedSzolgaltatasIds { get; set; } = new();
        #endregion

        #region Időpontjaim
        [BindProperty(SupportsGet = true)]
        public string? NaptarDatum { get; set; }

        [BindProperty]
        public string? MunkaidoKezdete { get; set; }

        [BindProperty]
        public string? MunkaidoVege { get; set; }

        [BindProperty]
        public string? SzunetKezdete { get; set; }

        [BindProperty]
        public string? SzunetVege { get; set; }

        [BindProperty]
        public List<string> SzunetekKezdete { get; set; } = new();

        [BindProperty]
        public List<string> SzunetekVege { get; set; } = new();

        public List<FodraszSzunet> BetoltSzunetek { get; set; } = new();
        #endregion

        #region FoglalasaimListak
        public List<Idopont> MaiFoglalasok { get; set; } = new();
        public List<Idopont> JovobeliFoglalasok { get; set; } = new();
        public List<Idopont> Regifoglalasok { get; set; } = new();
        #endregion

        #region PushErtesitesek
        public string? OneSignalExternalId { get; set; }
        public bool IsFodraszPushAllowed { get; set; }
        #endregion

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
            {
                FodraszProfil = null;
                return Page();
            }

            IsFodraszPushAllowed = true;
            OneSignalExternalId = $"fodrasz-{user.FodraszId.Value}";

            FodraszProfil = await _context.Fodraszok
                .Include(sz => sz.VallaltSzolgaltatasok)
                .FirstOrDefaultAsync(f => f.ID == user.FodraszId.Value);

            if (FodraszProfil != null)
            {
                EditNev = FodraszProfil.Nev;
                EditEmail = FodraszProfil.Email;
                EditTelefon = FodraszProfil.Telefon;
                EditSpecializacio = FodraszProfil.Specializacio;
                SelectedSzolgaltatasIds = FodraszProfil.VallaltSzolgaltatasok?
                    .Select(sz => sz.Id)
                    .ToList() ?? new List<int>();
            }

            OsszesSzolgaltatas = await _context.Szolgaltatasok
                .OrderBy(sz => sz.Nev)
                .ToListAsync();

            if (Section == "idopontjaim")
            {
                var datumBudapest = BudapestTime.TodayBudapestDate;

                if (!string.IsNullOrWhiteSpace(NaptarDatum) &&
                    DateTime.TryParseExact(
                        NaptarDatum,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsed))
                {
                    datumBudapest = parsed.Date;
                }

                NaptarDatum = datumBudapest.ToString("yyyy-MM-dd");

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
                    TempData["Error"] = "A kiválasztott dátum időzóna szempontból nem kezelhető.";
                    return Page();
                }

                var munkaido = await _context.FodraszMunkaidok
                    .FirstOrDefaultAsync(m =>
                        m.FodraszId == user.FodraszId.Value &&
                        m.Datum >= napKezdetUtc &&
                        m.Datum < kovetkezoNapKezdetUtc);

                if (munkaido != null)
                {
                    MunkaidoKezdete = munkaido.Kezdoido.ToString(@"hh\:mm");
                    MunkaidoVege = munkaido.ZaroIdo.ToString(@"hh\:mm");
                }
                else
                {
                    MunkaidoKezdete = "";
                    MunkaidoVege = "";
                }

                BetoltSzunetek = await _context.FodraszSzunetek
                    .Where(s =>
                        s.FodraszId == user.FodraszId.Value &&
                        s.Datum >= napKezdetUtc &&
                        s.Datum < kovetkezoNapKezdetUtc)
                    .OrderBy(s => s.KezdoIdo)
                    .ToListAsync();
            }

            if (Section == "foglalasaim")
            {
                var maBudapest = BudapestTime.TodayBudapestDate;

                var maiNapKezdetBudapest = DateTime.SpecifyKind(maBudapest.Date, DateTimeKind.Unspecified);
                var holnapNapKezdetBudapest = maiNapKezdetBudapest.AddDays(1);

                DateTime maiNapKezdetUtc;
                DateTime holnapNapKezdetUtc;

                try
                {
                    maiNapKezdetUtc = BudapestTime.BudapestLocalToUtc(maiNapKezdetBudapest);
                    holnapNapKezdetUtc = BudapestTime.BudapestLocalToUtc(holnapNapKezdetBudapest);
                }
                catch (ArgumentException)
                {
                    TempData["Error"] = "A mai nap időzóna szempontból nem kezelhető.";
                    return Page();
                }

                var osszes = await _context.Idopontok
                    .Include(i => i.Szolgaltatas)
                    .Where(i => i.FodraszId == user.FodraszId.Value)
                    .OrderBy(i => i.EsedekessegiIdopont)
                    .ToListAsync();

                MaiFoglalasok = osszes
                    .Where(i =>
                        i.EsedekessegiIdopont >= maiNapKezdetUtc &&
                        i.EsedekessegiIdopont < holnapNapKezdetUtc)
                    .ToList();

                JovobeliFoglalasok = osszes
                    .Where(i => i.EsedekessegiIdopont >= holnapNapKezdetUtc)
                    .ToList();

                Regifoglalasok = osszes
                    .Where(i => i.EsedekessegiIdopont < maiNapKezdetUtc)
                    .OrderByDescending(i => i.EsedekessegiIdopont)
                    .ToList();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAdataimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
                return RedirectToPage("/Account/FodraszFelulet", new { section = "adataim" });

            var fodrasz = await _context.Fodraszok
                .Include(f => f.VallaltSzolgaltatasok)
                .FirstOrDefaultAsync(f => f.ID == user.FodraszId.Value);

            if (fodrasz == null)
                return RedirectToPage("/Account/FodraszFelulet", new { section = "adataim" });

            fodrasz.Nev = (EditNev ?? "").Trim();
            fodrasz.Email = (EditEmail ?? "").Trim();
            fodrasz.Telefon = (EditTelefon ?? "").Trim();
            fodrasz.Specializacio = (EditSpecializacio ?? "").Trim();

            fodrasz.VallaltSzolgaltatasok.Clear();

            var szolgLista = await _context.Szolgaltatasok
                .Where(s => SelectedSzolgaltatasIds != null && SelectedSzolgaltatasIds.Contains(s.Id))
                .ToListAsync();

            foreach (var s in szolgLista)
                fodrasz.VallaltSzolgaltatasok.Add(s);

            await _context.SaveChangesAsync();

            user.Email = fodrasz.Email;
            user.UserName = fodrasz.Email;
            user.NormalizedEmail = _userManager.NormalizeEmail(fodrasz.Email);
            user.NormalizedUserName = _userManager.NormalizeEmail(fodrasz.Email);
            user.PhoneNumber = string.IsNullOrWhiteSpace(fodrasz.Telefon) ? null : fodrasz.Telefon;

            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(UjJelszo) && UjJelszo.Length >= 6)
            {
                await _userManager.RemovePasswordAsync(user);
                await _userManager.AddPasswordAsync(user, UjJelszo);
            }

            return RedirectToPage("/Account/FodraszFelulet", new { section = "adataim" });
        }

        #region Időpontjaim

        public async Task<IActionResult> OnPostSzabadnapAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
            {
                TempData["Error"] = "Nem található a felhasználó!";
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim" });
            }

            if (string.IsNullOrWhiteSpace(NaptarDatum) ||
                !DateTime.TryParseExact(
                    NaptarDatum,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var datumBudapest))
            {
                TempData["Error"] = "Válassz ki egy dátumot a naptárból!";
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim" });
            }

            datumBudapest = datumBudapest.Date;

            var napKezdetBudapest = DateTime.SpecifyKind(datumBudapest.Date, DateTimeKind.Unspecified);
            var kovetkezoNapKezdetBudapest = napKezdetBudapest.AddDays(1);

            DateTime napKezdetUtc;
            DateTime kovetkezoNapKezdetUtc;

            try
            {
                napKezdetUtc = BudapestTime.BudapestLocalToUtc(napKezdetBudapest);
                kovetkezoNapKezdetUtc = BudapestTime.BudapestLocalToUtc(kovetkezoNapKezdetBudapest);
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim" });
            }

            var munkaidok = await _context.FodraszMunkaidok
                .Where(m =>
                    m.FodraszId == user.FodraszId.Value &&
                    m.Datum >= napKezdetUtc &&
                    m.Datum < kovetkezoNapKezdetUtc)
                .ToListAsync();

            var szunetek = await _context.FodraszSzunetek
                .Where(s =>
                    s.FodraszId == user.FodraszId.Value &&
                    s.Datum >= napKezdetUtc &&
                    s.Datum < kovetkezoNapKezdetUtc)
                .ToListAsync();

            if (munkaidok.Any() || szunetek.Any())
            {
                _context.FodraszMunkaidok.RemoveRange(munkaidok);
                _context.FodraszSzunetek.RemoveRange(szunetek);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Töröltük az elmentett időpontokat, ezen felül nincs semmi teendőd!<br>Nyugodtan hagyd üresen a mezőket és a mentés gombra sem szükséges rákattintanod!";
            }
            else
            {
                TempData["Info"] = "Szabadnap esetén nincs semmi teendőd!<br>Nyugodtan hagyd üresen a mezőket és a mentés gombra sem szükséges rákattintanod!";
            }

            return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim", naptarDatum = NaptarDatum });
        }

        public async Task<IActionResult> OnPostMentesAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
            {
                TempData["Error"] = "Nem található a felhasználó!";
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim" });
            }

            if (string.IsNullOrWhiteSpace(NaptarDatum) ||
                !DateTime.TryParseExact(
                    NaptarDatum,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var datumBudapest))
            {
                TempData["Error"] = "Válassz ki egy dátumot a naptárból!";
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim" });
            }

            datumBudapest = datumBudapest.Date;

            var napKezdetBudapest = DateTime.SpecifyKind(datumBudapest.Date, DateTimeKind.Unspecified);

            DateTime napKezdetUtc;
            try
            {
                napKezdetUtc = BudapestTime.BudapestLocalToUtc(napKezdetBudapest);
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim", naptarDatum = NaptarDatum });
            }

            if (!TimeSpan.TryParse(MunkaidoKezdete, out var kezdo) ||
                !TimeSpan.TryParse(MunkaidoVege, out var zaro) ||
                kezdo >= zaro)
            {
                TempData["Error"] = "Érvénytelen a megadott munkaidő kezdete és vége kombináció!";
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim", naptarDatum = NaptarDatum });
            }

            var munkaido = await _context.FodraszMunkaidok
                .FirstOrDefaultAsync(mi =>
                    mi.FodraszId == user.FodraszId.Value &&
                    mi.Datum == napKezdetUtc);

            if (munkaido == null)
            {
                munkaido = new FodraszMunkaIdo
                {
                    FodraszId = user.FodraszId.Value,
                    Datum = napKezdetUtc
                };
                _context.FodraszMunkaidok.Add(munkaido);
            }

            munkaido.Kezdoido = kezdo;
            munkaido.ZaroIdo = zaro;

            var regiSzunetek = await _context.FodraszSzunetek
                .Where(sz => sz.FodraszId == user.FodraszId.Value && sz.Datum == napKezdetUtc)
                .ToListAsync();

            _context.FodraszSzunetek.RemoveRange(regiSzunetek);

            var kezdok = SzunetekKezdete ?? new List<string>();
            var vegek = SzunetekVege ?? new List<string>();

            for (int i = 0; i < kezdok.Count && i < vegek.Count; ++i)
            {
                if (!TimeSpan.TryParse(kezdok[i], out var sk) || !TimeSpan.TryParse(vegek[i], out var sv))
                    continue;

                if (sk >= sv)
                    continue;

                if (sk < kezdo || sv > zaro)
                    continue;

                _context.FodraszSzunetek.Add(new FodraszSzunet
                {
                    FodraszId = user.FodraszId.Value,
                    Datum = napKezdetUtc,
                    KezdoIdo = sk,
                    ZaroIdo = sv
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Sikeres mentés";

            return RedirectToPage("/Account/FodraszFelulet", new
            {
                section = "idopontjaim",
                naptarDatum = NaptarDatum
            });
        }

        public async Task<IActionResult> OnGetCopyPrevDayAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim" });

            var datumBudapest = BudapestTime.TodayBudapestDate;

            if (!string.IsNullOrWhiteSpace(NaptarDatum) &&
                DateTime.TryParseExact(
                    NaptarDatum,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
            {
                datumBudapest = parsed.Date;
            }

            var celNapKezdetBudapest = DateTime.SpecifyKind(datumBudapest.Date, DateTimeKind.Unspecified);

            DateTime celNapKezdetUtc;
            try
            {
                celNapKezdetUtc = BudapestTime.BudapestLocalToUtc(celNapKezdetBudapest);
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToPage("/Account/FodraszFelulet", new
                {
                    section = "idopontjaim",
                    naptarDatum = datumBudapest.ToString("yyyy-MM-dd")
                });
            }

            var elozoMunkaido = await _context.FodraszMunkaidok
                .Where(i => i.FodraszId == user.FodraszId.Value)
                .OrderByDescending(i => i.Datum)
                .FirstOrDefaultAsync();

            if (elozoMunkaido == null)
            {
                TempData["Info"] = "Nem található előzőleg megadott munkaidő.";
                return RedirectToPage("/Account/FodraszFelulet", new
                {
                    section = "idopontjaim",
                    naptarDatum = datumBudapest.ToString("yyyy-MM-dd")
                });
            }

            var elozoUtc = elozoMunkaido.Datum;

            var elozoSzunetek = await _context.FodraszSzunetek
                .Where(sz => sz.FodraszId == user.FodraszId.Value && sz.Datum == elozoUtc)
                .ToListAsync();

            var meglvoCelMunkaido = await _context.FodraszMunkaidok
                .FirstOrDefaultAsync(m =>
                    m.FodraszId == user.FodraszId.Value &&
                    m.Datum == celNapKezdetUtc);

            if (meglvoCelMunkaido == null)
            {
                meglvoCelMunkaido = new FodraszMunkaIdo
                {
                    FodraszId = user.FodraszId.Value,
                    Datum = celNapKezdetUtc
                };
                _context.FodraszMunkaidok.Add(meglvoCelMunkaido);
            }

            meglvoCelMunkaido.Kezdoido = elozoMunkaido.Kezdoido;
            meglvoCelMunkaido.ZaroIdo = elozoMunkaido.ZaroIdo;

            var celNapiRegiSzunetek = await _context.FodraszSzunetek
                .Where(sz => sz.FodraszId == user.FodraszId.Value && sz.Datum == celNapKezdetUtc)
                .ToListAsync();

            _context.FodraszSzunetek.RemoveRange(celNapiRegiSzunetek);

            foreach (var s in elozoSzunetek)
            {
                _context.FodraszSzunetek.Add(new FodraszSzunet
                {
                    FodraszId = user.FodraszId.Value,
                    Datum = celNapKezdetUtc,
                    KezdoIdo = s.KezdoIdo,
                    ZaroIdo = s.ZaroIdo
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Sikeres mentés";
            return RedirectToPage("/Account/FodraszFelulet", new
            {
                section = "idopontjaim",
                naptarDatum = datumBudapest.ToString("yyyy-MM-dd")
            });
        }

        #endregion

        #region Foglalasaim

        public async Task<IActionResult> OnPostTorolFoglalasAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.FodraszId == null)
                return RedirectToPage("/Account/FodraszFelulet", new { section = "foglalasaim" });

            var idopont = await _context.Idopontok
                .Include(i => i.Szolgaltatas)
                .FirstOrDefaultAsync(i => i.ID == id && i.FodraszId == user.FodraszId.Value);

            if (idopont == null)
                return RedirectToPage("/Account/FodraszFelulet", new { section = "foglalasaim" });

            var archiv = new ToroltIdopont
            {
                EredetiIdopontId = idopont.ID,
                FodraszId = idopont.FodraszId,
                SzolgaltatasId = idopont.SzolgaltatasId,
                EsedekessegiIdopont = idopont.EsedekessegiIdopont,
                FoglalasiIdopont = idopont.FoglalasiIdopont,
                CustomerNeve = idopont.CustomerNeve,
                CustomerEmail = idopont.CustomerEmail,
                CustomerPhone = idopont.CustomerPhone,
                CustomerNotes = idopont.CustomerNotes,
                TorolveUtc = DateTime.UtcNow,
                KiTorolte = user.Email ?? string.Empty
            };

            _context.ToroltIdopontok.Add(archiv);
            _context.Remove(idopont);
            await _context.SaveChangesAsync();

            var subject = "BestBarberShop - foglalás törölve";

            var hu = CultureInfo.GetCultureInfo("hu-HU");
            var toroltIdopontBudapest = BudapestTime.UtcToBudapest(idopont.EsedekessegiIdopont);
            var toroltIdopontSzoveg = toroltIdopontBudapest.ToString("MMMM d. (dddd) HH:mm", hu);

            var szolgaltatas = WebUtility.HtmlEncode(idopont.Szolgaltatas?.Nev ?? "");
            var nev = WebUtility.HtmlEncode(idopont.CustomerNeve ?? "");

            var ujIdopontUrl = "https://bestbarbershopbookingsystem-djf9c0hch0dqexdt.westeurope-01.azurewebsites.net/";
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
    <h2>Kedves {nev}!</h2>
    <p>Foglalásod törlésre került!</p>
    <p>Részletek:</p>
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""margin: 0 auto; max-width: 320px;"">
        <tr>
            <td style=""padding: 10px 18px; text-align: center; border-radius: 15px; background-color: #e8dcc8; background-image: linear-gradient(to top right, rgba(191,162,122,0.7), #ffffff);"">
                {szolgaltatas}<br />
                {toroltIdopontSzoveg}
            </td>
        </tr>
    </table>
    <p>
        <a href=""{ujIdopontUrl}"" style=""text-decoration: none; color: white;"">
            <span style=""background:rgba(191, 162, 122, 0.7); display:inline-block; margin: 0 auto; padding: 7px 14px; border-radius: 10px;"">
                új foglalás
            </span>
        </a>
    </p>
    <p style=""padding-top: 20px;"">
        BestBarbershop<br />
        <a href=""{maps}"" style=""color:black;"">1115 Budapest Bártfai utca 38</a><br />
        <a href=""mailto:szaszakpepe@gmail.com"" style=""text-decoration:none; color:black;"">szaszakpepe@gmail.com</a><br />
        <a href=""tel:+36307271232"" style=""color:black;"">+36 30 727 1232</a>
    </p>
</div>
</body>
</html>";

            await _emailKuldo.SendAsync(idopont.CustomerEmail, subject, body);

            return RedirectToPage("/Account/SikerFodraszTorles", new { id = archiv.Id });
        }

        #endregion
    }
}