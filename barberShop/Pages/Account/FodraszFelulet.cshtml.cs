using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using System.Globalization;
using System.Net;
using static barberShop.Pages.Account.FodraszFeluletModel;

namespace barberShop.Pages.Account
{
    [Authorize(Roles = "Fodrasz")]
    public class FodraszFeluletModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Felhasznalo> _userManager;
        private readonly IEmailKuldo _emailKuldo;
        private readonly IWebHostEnvironment _env;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IBackgroundTaskQueue _taskQueue;

        private static readonly string[] Kiterjesztesek = [".jpg", ".jpeg", ".png", ".webp", ".avif"];
        private const long MaxMeret = 5 * 1024 * 1024;
        public FodraszFeluletModel(
            AppDbContext context,
            UserManager<Felhasznalo> userManager,
            IEmailKuldo emailKuldo, IWebHostEnvironment env,
            IPushNotificationService pushNotificationService,
            IBackgroundTaskQueue taskQueue)
        {
            _context = context;
            _userManager = userManager;
            _emailKuldo = emailKuldo;
            _env = env;
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

        [BindProperty]
        public IFormFile? EditKep { get; set; }

        [BindProperty]
        public string? KepFajlNeve { get; set; }
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

        public List<Szolgaltatas> Szolgaltatasok { get; set; } = new();
        public List<KorabbiUgyfelVM> KorabbiUgyfelek { get; set; } = new();

        public class KorabbiUgyfelVM
        {
            public string Nev { get; set; } = "";
            public string Email { get; set; } = "";
            public string? Telefon { get; set; }
            public DateTime UtolsoFoglalas { get; set; }
        }

        public bool FoglalasKartyaNyitva { get; set; } = false;

        [BindProperty]
        public string? UgyfelEmail { get; set; }

        [BindProperty]
        public int? SzolgaltatasId { get; set; }
        [BindProperty]
        public string? FoglDatum { get; set; }
        [BindProperty]
        public string? FoglIdopont { get; set; }
        [BindProperty]
        public string? UgyfelNev { get; set; }
        [BindProperty]
        public string? UgyfelTelefon { get; set; }
        [BindProperty]
        public string? UgyfelMegjegyzes { get; set; }
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
                KepFajlNeve = FodraszProfil.ProfilkepFajlNeve;
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

                Szolgaltatasok = await _context.Szolgaltatasok
                    .OrderBy(x => x.Sorszam)
                    .OrderBy(x => x.Nev)
                    .ToListAsync();

                var alap = await _context.Idopontok
                    .AsNoTracking()
                    .Where(i => i.FodraszId == user.FodraszId && !string.IsNullOrWhiteSpace(i.CustomerEmail))
                    .Select(i => new
                    {
                        i.CustomerEmail,
                        i.CustomerPhone,
                        i.CustomerNeve,
                        i.FoglalasiIdopont
                    })
                    .ToListAsync();

                KorabbiUgyfelek = alap
                    .GroupBy(i => i.CustomerEmail)
                    .Select(g => g.OrderByDescending(x => x.FoglalasiIdopont).First())
                    .Select(x => new KorabbiUgyfelVM
                    {
                        Nev = x.CustomerNeve ?? "",
                        Email = x.CustomerEmail ?? "",
                        Telefon = x.CustomerPhone,
                        UtolsoFoglalas = x.FoglalasiIdopont
                    })
                    .OrderBy(x => x.Nev)
                    .ToList();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAdataimAsync(IFormFile? EditKep)
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

            if (EditKep != null && EditKep.Length > 0)
            {
                var (error, fileName) = await TrySaveFodraszKepAsync(fodrasz.ID, EditKep, fodrasz.ProfilkepFajlNeve);
                if (error != null)
                {
                    ModelState.AddModelError(string.Empty, $"Kép feltöltési hiba: {error}");
                    return Page();
                }

                if (!string.IsNullOrWhiteSpace(fileName))
                    fodrasz.ProfilkepFajlNeve = fileName;
            }

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

       
        public async Task<IActionResult> OnPostManualFoglalasAsync()
        {
            Section = "foglalasaim";

            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
            {
                TempData["Error"] = "Nem található a fodrász felhasználó!";
                return RedirectToPage("/Account/FodraszFelulet", new { section = "foglalasaim" });
            }

            if (!SzolgaltatasId.HasValue || string.IsNullOrWhiteSpace(FoglDatum) ||string.IsNullOrWhiteSpace(FoglIdopont) || string.IsNullOrWhiteSpace(UgyfelNev) || string.IsNullOrWhiteSpace(UgyfelEmail) 
                || string.IsNullOrWhiteSpace(UgyfelTelefon))
            {
                ModelState.AddModelError(string.Empty, "Hiányzó kötelező mező! (szolgáltatás, dátum, időpont, név, email)");
                FoglalasKartyaNyitva = true;
                await OnGetAsync();
                return Page();
            }

            if (!DateTime.TryParseExact($"{FoglDatum} {FoglIdopont}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var kezdesBudapest))
            {
                ModelState.AddModelError(string.Empty, "Hibás dátum/idő formátum!");
                FoglalasKartyaNyitva = true;
                await OnGetAsync();
                return Page();
            }

            DateTime kezdesUtc;
            try
            {
                kezdesUtc = BudapestTime.BudapestLocalToUtc(DateTime.SpecifyKind(kezdesBudapest, DateTimeKind.Unspecified));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await OnGetAsync();
                return Page();
            }

            var szolg = await _context.Szolgaltatasok.FindAsync(SzolgaltatasId.Value);
            if (szolg == null)
            {
                ModelState.AddModelError(string.Empty, "A megadott szolgáltatás nem található!");
                FoglalasKartyaNyitva = true;
                await OnGetAsync();
                return Page();
            }

            if (await _context.Idopontok.AnyAsync(i =>
        i.FodraszId == user.FodraszId.Value &&
        i.EsedekessegiIdopont == kezdesUtc))
            {
                ModelState.AddModelError(string.Empty, "Erre az időpontra már van foglalás!");
                FoglalasKartyaNyitva = true;
                await OnGetAsync();
                return Page();
            }

            var idopont = new Idopont
            {
                FodraszId = user.FodraszId.Value,
                SzolgaltatasId = szolg.Id,
                EsedekessegiIdopont = kezdesUtc,
                FoglalasiIdopont = DateTime.UtcNow,
                CustomerNeve= UgyfelNev.Trim(),
                CustomerEmail = UgyfelEmail.Trim(),
                CustomerPhone = UgyfelTelefon.Trim(),
                CustomerNotes = string.IsNullOrWhiteSpace(UgyfelMegjegyzes) ? null : UgyfelMegjegyzes.Trim()
            };


            _context.Idopontok.Add(idopont);
            await _context.SaveChangesAsync();

            var fodr = await _context.Fodraszok.FindAsync(user.FodraszId.Value);

            var subject = "BestBarberShop - foglalás visszaigazolása";

            var hu = CultureInfo.GetCultureInfo("hu-HU");
            var idopontSzoveg = kezdesBudapest.ToString("MMMM d. (dddd) HH:mm", hu);
            var arSzoveg = szolg.Ar.ToString("N0", hu);
            var nevH = WebUtility.HtmlEncode(UgyfelNev);
            var szolgNevH = WebUtility.HtmlEncode(szolg.Nev);
            var szolgIdo = WebUtility.HtmlEncode(szolg.Idotartam.ToString());

            var baseUrl = "https://bestbarbershopbookingsystem-djf9c0hch0dqexdt.westeurope-01.azurewebsites.net/"; // saját domain
            var profilKepUrl = !string.IsNullOrWhiteSpace(fodr.ProfilkepFajlNeve)
                ? $"{baseUrl}/kepek/fodraszok/{WebUtility.UrlEncode(fodr.ProfilkepFajlNeve)}"
                : $"{baseUrl}/kepek/backgrounds/logo.webp";
            var fodrNev = WebUtility.HtmlDecode(fodr.Nev);

            var maps = "https://www.bing.com/maps/search?mepi=72%7ELocal%7EEmbedded%7EEntity_Vertical_List_Card&ty=17&poicount=18&sei=0&FORM=MPSRPL&q=kelenf%C3%B6ld+fodr%C3%A1szat&secq=%C3%9Ajhull%C3%A1m+Fodr%C3%A1szat+kelenfoeld+fodraszat&sece=ypid.YN8081x11846474530400285953&ppois=47.467506408691406_19.035743713378906_%C3%9Ajhull%C3%A1m+Fodr%C3%A1szat_YN8081x11846474530400285953%7E47.46304702758789_19.034894943237305_X%C3%A9nia+Fodr%C3%A1szat_YN8081x14308692530027957564%7E47.46721649169922_19.042898178100586_B%C3%A1rtfai+Sz%C3%A9ps%C3%A9gszalon+most_YN8081x3342422111653719704%7E&segment=Local&cp=47.467179%7E19.036090&lvl=17.7&style=r";

            var body = $@"
<html lang=""hu"">
<head>
    <meta charset=""utf-8"" />
    <style type=""text/css"">
        body {{ font-family: Arial, Helvetica, sans-serif; color: #333; color: black; }}
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
                {szolgIdo}perc<br />
                {idopontSzoveg}<br />
                {arSzoveg} Ft
            </td>
        </tr>
    </table>
    <p>{fodrNev}</p>
    <img src=""{profilKepUrl}"" alt=""{fodrNev}"" style=""width:120px;height:120px;object-fit:cover;border-radius:50%;border:2px solid #e8dcc8;"" />
    <p>
        BestBarbershop<br />
        <a href=""{maps}"" style=""color: black;"">1115 Budapest Bártfai utca 38</a><br />
        <a href=""mailto:szaszakpepe@gmail.com"" style=""text-decoration: none; color: black;"">szaszakpepe@gmail.com</a><br />
        <a href=""tel:+36307271232"" style=""text-decoration: none; color: black;"">+36 30 727 1232</a>
    </p>
</div>
</body>
</html>";

            await _emailKuldo.SendAsync(UgyfelEmail, subject, body);


            var barberSubject = $"Új foglalás | {idopontSzoveg}";
            var barberemailBody = $@"
<html lang=""hu"">
<head>
    <meta charset=""utf-8"" />
    <style type=""text/css"">
        body {{ font-family: Arial, Helvetica, sans-serif; color: #333; color: black; }}
        h2 {{ color: rgba(191, 162, 122, 0.7); }}
    </style>
</head>
<body>
<div style=""text-align: center;"">
    <h2>Kedves {fodrNev}!</h2>
    <p>Új foglalásod érkezett!</p>
    <p>Foglalásod részletei:</p>
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""margin: 0 auto; max-width: 320px;"">
        <tr>
            <td style=""padding: 10px 18px; text-align: center; border-radius: 15px; background-color: #e8dcc8; background-image: linear-gradient(to top right, rgba(191, 162, 122, 0.7) 0%, rgb(252, 251, 249) 59%, rgb(255, 255, 255) 100%); border: solid 0.5px #eceae6;"">
                {nevH}<br />
                {szolgNevH}<br />
                {idopontSzoveg}<br />
            </td>
        </tr>
    </table>
    <p>
        BestBarbershop<br />
        <a href=""{maps}"" style=""color: black;"">1115 Budapest Bártfai utca 38</a><br />
        <a href=""mailto:szaszakpepe@gmail.com"" style=""text-decoration: none; color: black;"">szaszakpepe@gmail.com</a><br />
        <a href=""tel:+36307271232"" style=""text-decoration: none; color: black;"">+36 30 727 1232</a>
    </p>
</div>
</body>
</html>";

            await _taskQueue.QueueBackgroundWorkItemAsync(async ct =>
            {
                await _emailKuldo.SendAsync(fodr.Email, barberSubject, barberemailBody);
            });


            var barberExternalId = $"fodrasz-{fodr.ID}";
            await _pushNotificationService.SendBookingToBarberAsync(barberExternalId, UgyfelNev, szolg.Nev, idopont.EsedekessegiIdopont);

            TempData["Success"] = "Foglalás sikeresen rögzítve!";
            return RedirectToPage("/Account/FodraszFelulet", new { section = "foglalasaim" });
        }


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

        private async Task<(string? Error, string? FileName)> TrySaveFodraszKepAsync(int fodraszId, IFormFile? file, string? korabbFileNev)
        {
            if (file.Length == 0)
                return (null, null);
            if (file.Length > MaxMeret)
                return ("max. 5 MB.", null);
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !Kiterjesztesek.Contains(ext))
                return ("csak jpg/png/webp/avif.", null);
            var dir = Path.Combine(_env.WebRootPath, "kepek", "fodraszok");
            Directory.CreateDirectory(dir);
            var ujNev = $"fodrasz-{fodraszId}-{Guid.NewGuid():N}.webp";
            var teljesUt = Path.Combine(dir, ujNev);
            try
            {
                await using var bemenet = file.OpenReadStream();
                using var image = await Image.LoadAsync(bemenet);
                await image.SaveAsync(teljesUt, new WebpEncoder { Quality = 82 });
            }
            catch (Exception)
            {
                return ("érvénytelen kép", null);
            }
            if(!string.IsNullOrWhiteSpace(korabbFileNev))
                TorolFodraszKep(korabbFileNev);

            return (null, ujNev);
        }

        private void TorolFodraszKep(string? fileNev)
        {
            if (string.IsNullOrWhiteSpace(fileNev))
                return;

            var fajlNevNorm = Path.GetFileName(fileNev);
            if (string.IsNullOrEmpty(fajlNevNorm))
                return;

            var teljes = Path.GetFullPath(Path.Combine(_env.WebRootPath, "kepek", "fodraszok", fajlNevNorm));

            try
            {
                if(System.IO.File.Exists(teljes))
                    System.IO.File.Delete(teljes);
            }
            catch
            {
            }
        }
    }
}