using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace barberShop.Pages.Account
{
    [Authorize(Roles = "Fodrasz")]
    public class FodraszFeluletModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Felhasznalo> _userManager;

        public FodraszFeluletModel(AppDbContext context, UserManager<Felhasznalo> userManager)
        {
            _context = context;
            _userManager = userManager;
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
        public List<FodraszSzunet> BetoltSzunetek { get; set; } = new();
        #endregion

        #region
            public List<Idopont> MaiFoglalasok { get;set;  } = new();
            public List<Idopont> JovobeliFoglalasok { get; set; } = new();
            public List<Idopont> Regifoglalasok { get; set; } = new();

        #endregion
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
            {
                FodraszProfil = null;
                return Page();
            }

            FodraszProfil = await _context.Fodraszok
                .Include(sz => sz.VallaltSzolgaltatasok)
                .FirstOrDefaultAsync(f => f.ID == user.FodraszId);

            if (FodraszProfil != null)
            {
                EditNev = FodraszProfil.Nev;
                EditEmail = FodraszProfil.Email;
                EditTelefon = FodraszProfil.Telefon;
                EditSpecializacio = FodraszProfil.Specializacio;
                SelectedSzolgaltatasIds = FodraszProfil.VallaltSzolgaltatasok?.Select(sz => sz.Id).ToList() ?? new List<int>();
            }

            OsszesSzolgaltatas = await _context.Szolgaltatasok.OrderBy(sz => sz.Nev).ToListAsync();

            if (Section == "idopontjaim" && user.FodraszId != null)
            {
                var datum = DateTime.Today;
                if (!string.IsNullOrWhiteSpace(NaptarDatum) && DateTime.TryParse(NaptarDatum, out var parsed))
                    datum = parsed.Date;

                NaptarDatum = datum.ToString("yyyy-MM-dd");

                var munkaido = await _context.FodraszMunkaidok
                    .FirstOrDefaultAsync(m => m.FodraszId == user.FodraszId && m.Datum == datum);
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
                    .Where(s => s.FodraszId == user.FodraszId && s.Datum == datum)
                    .OrderBy(s => s.KezdoIdo)
                    .ToListAsync();
            }

            if (Section == "foglalasaim" && user.FodraszId != null)
            {
                var today = DateTime.Today;

                var osszes = await _context.Idopontok
                    .Include(i => i.Szolgaltatas)
                    .Where(i => i.FodraszId == user.FodraszId)
                    .OrderBy(i => i.EsedekessegiIdopont)
                    .ToListAsync();

                MaiFoglalasok = osszes
                    .Where(i => i.EsedekessegiIdopont.Date == today)
                    .ToList();

                JovobeliFoglalasok = osszes
                    .Where(i => i.EsedekessegiIdopont.Date > today)
                    .ToList();

                Regifoglalasok = osszes
                    .Where(i => i.EsedekessegiIdopont.Date < today)
                    .OrderByDescending(i=>i.EsedekessegiIdopont)
                    .ToList();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAdataimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
                return RedirectToPage("/Account/FodraszFelulet", new { section = "adataim" });

            var fodrasz = await _context.Fodraszok.Include(f => f.VallaltSzolgaltatasok).FirstOrDefaultAsync(f => f.ID == user.FodraszId);
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
                TempData["Error"] = "Nem található a felhasználó !";
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim" });
            }

            if (string.IsNullOrWhiteSpace(NaptarDatum) ||!DateTime.TryParse(NaptarDatum, out var datum))
            {
                TempData["Error"] = "Válassz ki egy dátumot a naptárból !";
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim" });
            }

            datum = datum.Date;

            var munkaidok = await _context.FodraszMunkaidok
                .Where(m => m.FodraszId == user.FodraszId && m.Datum == datum)
                .ToListAsync();

            var szunetek = await _context.FodraszSzunetek
                .Where(s => s.FodraszId == user.FodraszId && s.Datum == datum)
                .ToListAsync();

            if (munkaidok.Any() || szunetek.Any())
            {
                _context.FodraszMunkaidok.RemoveRange(munkaidok);
                _context.FodraszSzunetek.RemoveRange(szunetek);
                TempData["Success"] = "Töröltük az elmentett időpontokat, ezen felül nincs semmi teendőd!<br>Nyugodtan hagyd üresen a mezőket és a mentés gombra sem szükséges rákattintanod!";
                await _context.SaveChangesAsync();
            }
            else
            {
                TempData["Info"] = "Szabadnap esetén nincs semmi teendőd!<br>Nyugodtan hagyd üresen a mezőket és a mentés gombra sem szükséges rákattintanod!";
                
            }


            return RedirectToPage("/Account/FodraszFelulet", new { Section = "idopontjaim", naptarDatum = NaptarDatum });
        }
        public async Task<IActionResult> OnPostMentesAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
            {
                TempData["Error"] = "Nem található a felhasználó +";
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim" });
            }
            
            if (string.IsNullOrWhiteSpace(NaptarDatum) || !DateTime.TryParse(NaptarDatum, out var datum))
            {
                TempData["Error"] = "Válassz ki egy dátumot a naptárból !";
                return RedirectToPage("/Account/FodraszFelulet", new { Section = "idopontjaim" });
            }

            datum = datum.Date;
            if (!TimeSpan.TryParse(MunkaidoKezdete, out var kezdo) || !TimeSpan.TryParse(MunkaidoVege,out var zaro) || kezdo >=zaro)
            {
                TempData["Error"] = "Érvénytelen a megadott munkaidő kezdete és vége kombináció !";
                return RedirectToPage("/Account/FodraszFelulet", new { Section = "idopontjaim" });
            }

            var munkaido = await _context.FodraszMunkaidok
                .FirstOrDefaultAsync(mi => mi.FodraszId == user.FodraszId && mi.Datum == datum);
            if (munkaido == null)
            {
                munkaido = new FodraszMunkaIdo { FodraszId = user.FodraszId.Value, Datum = datum };
                _context.FodraszMunkaidok.Add(munkaido);
            }
            munkaido.Kezdoido = kezdo;
            munkaido.ZaroIdo = zaro;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Sikeres mentés";
            return RedirectToPage("/Account/FodraszFelulet", new {Section="idopontjaim", naptarDatum =NaptarDatum});
        }

        public async Task<IActionResult> OnPostAddSzunetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim" });

            if (string.IsNullOrWhiteSpace(NaptarDatum) || !DateTime.TryParse(NaptarDatum, out var datum))
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim", naptarDatum = NaptarDatum });
            datum = datum.Date;

            if (string.IsNullOrWhiteSpace(SzunetKezdete) || string.IsNullOrWhiteSpace(SzunetVege) ||
                !TimeSpan.TryParse(SzunetKezdete, out var kezdo) || !TimeSpan.TryParse(SzunetVege, out var zaro))
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim", naptarDatum = NaptarDatum });

            _context.FodraszSzunetek.Add(new FodraszSzunet
            {
                FodraszId = user.FodraszId.Value,
                Datum = datum,
                KezdoIdo = kezdo,
                ZaroIdo = zaro
            });
            await _context.SaveChangesAsync();
            return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim", naptarDatum = NaptarDatum });
        }

        public async Task<IActionResult> OnPostDeleteSzunetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim", naptarDatum = NaptarDatum });

            var sz = await _context.FodraszSzunetek.FirstOrDefaultAsync(s => s.ID == id && s.FodraszId == user.FodraszId);
            if (sz != null)
            {
                _context.FodraszSzunetek.Remove(sz);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim", naptarDatum = NaptarDatum });
        }

        public async Task<IActionResult> OnGetCopyPrevDayAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
                return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim" });

            var datum = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(NaptarDatum) && DateTime.TryParse(NaptarDatum, out var parsed))
                datum = parsed.Date;
            var elozo = datum.AddDays(-1);

            var elozoMunkaido = await _context.FodraszMunkaidok
                .FirstOrDefaultAsync(m => m.FodraszId == user.FodraszId && m.Datum == elozo);
            var elozoSzunetek = await _context.FodraszSzunetek
                .Where(s => s.FodraszId == user.FodraszId && s.Datum == elozo)
                .ToListAsync();

            if (elozoMunkaido != null)
            {
                var m = await _context.FodraszMunkaidok.FirstOrDefaultAsync(m => m.FodraszId == user.FodraszId && m.Datum == datum);
                if (m == null)
                {
                    m = new FodraszMunkaIdo { FodraszId = user.FodraszId.Value, Datum = datum };
                    _context.FodraszMunkaidok.Add(m);
                }
                m.Kezdoido = elozoMunkaido.Kezdoido;
                m.ZaroIdo = elozoMunkaido.ZaroIdo;
               
            }
            foreach (var s in elozoSzunetek)
            {
                _context.FodraszSzunetek.Add(new FodraszSzunet
                {
                    FodraszId = user.FodraszId.Value,
                    Datum = datum,
                    KezdoIdo = s.KezdoIdo,
                    ZaroIdo = s.ZaroIdo
                });
            }
            await _context.SaveChangesAsync();
            return RedirectToPage("/Account/FodraszFelulet", new { section = "idopontjaim", naptarDatum = datum.ToString("yyyy-MM-dd") });
        }
        #endregion
    }
}