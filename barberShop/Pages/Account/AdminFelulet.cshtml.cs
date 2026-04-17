using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace barberShop.Pages.Account
{
    /// <summary>Csak Admin szerepkörrel érhető el az oldal.</summary>
    [Authorize(Roles = "Admin")]
    public class AdminFeluletModel : PageModel
    {
        /// <summary>EF Core kontextus: adatbázis elérés.</summary>
        private readonly AppDbContext _context;
        private readonly UserManager<Felhasznalo> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] SzolgKepKiterjesztesek = [".jpg", ".jpeg", ".png", "webp", ".avif"];
        private const long SzolgKepMaxMeretBajt = 5 * 1024 * 1024;
        public AdminFeluletModel(AppDbContext context, UserManager<Felhasznalo> userManager, RoleManager<IdentityRole> roleManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _env = env;
        }

        /// <summary>Az oldalon megjelenő szolgáltatások listája.</summary>
        public List<Szolgaltatas> SzolgaltatasLista { get; set; } = new();


        [BindProperty(SupportsGet =true)]
        public string Section { get; set; } = "";


        /// <summary>URL-ből jön: ?editId=5 → az 5-ös id-jű szolgáltatás szerkesztése (sárga sor a nézetben). SupportsGet: GET kérésből is kötődik.</summary>
        [BindProperty(SupportsGet = true)]
        public int? EditId { get; set; }

        /// <summary>Új szolgáltatás űrlap mezői (Create handler kapja).</summary>
        [BindProperty(SupportsGet = false)]
        public string CreateNev { get; set; } = "";
        [BindProperty(SupportsGet = false)]
        public int CreateIdotartam { get; set; } = 30;
        [BindProperty(SupportsGet = false)]
        public decimal CreateAr { get; set; }
        [BindProperty(SupportsGet = false)]
        public string CreateLeiras { get; set; } = "";

        /// <summary>GET: lista betöltése; EditId a query/route-ból automatikusan kötődik.</summary>
        

        // Fodrasz szekció - Props
        public List<Fodrasz> FodraszLista { get; set; } = new();

        [BindProperty]
        public string CreateFodraszNev { get; set; } = "";

        [BindProperty]
        public string CreateFodraszEmail { get; set; } = "";

        [BindProperty]
        public string CreateFodraszTelefon { get; set; } = "";

        [BindProperty]
        public string CreateFodraszSpecializacio { get; set; } = "";


        // Felhasználók szekció - Props
        public List<Felhasznalo> Adminok { get; set; } = new();
        public List<Felhasznalo> Fodraszok { get; set; } = new();
        public List<Felhasznalo> Muglik { get; set; } = new();

        [BindProperty]
        public string CreateUserEmail { get; set; }

        [BindProperty]
        public string CreateUserPassword { get; set; }

        [BindProperty]
        public string CreateUserRole { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ResetPasswordUserId { get; set; } = "";



        public async Task OnGetAsync()
        {
            SzolgaltatasLista = await _context.Szolgaltatasok.OrderBy(s => s.Nev).ToListAsync();
            FodraszLista = await _context.Fodraszok
                .Include(s => s.VallaltSzolgaltatasok)
                .OrderBy(s => s.Nev)
                .ToListAsync();

            var osszesUser = _userManager.Users.ToList();
            Adminok = new List<Felhasznalo>();
            Fodraszok = new List<Felhasznalo>();
            Muglik = new List<Felhasznalo>();
            foreach (var felhasz in osszesUser)
            {
                if (felhasz.Email == "kerberosz@kerberosz.com")
                    continue;
                
                var roles = await _userManager.GetRolesAsync(felhasz);
                if (roles.Contains("Admin"))
                    Adminok.Add(felhasz);
                else if (roles.Contains("Fodrasz"))
                    Fodraszok.Add(felhasz);
                else if (roles.Contains("Mugli"))
                    Muglik.Add(felhasz);
            }
        }

        /// <summary>POST Create: új szolgáltatás mentése, majd vissza ugyanerre az oldalra.</summary>
        public async Task<IActionResult> OnPostCreateAsync(IFormFile? CreateKepSotet, IFormFile? CreateKepVilagos)
        {
            if (!string.IsNullOrWhiteSpace(CreateNev))
            {
                var uj = new Szolgaltatas
                {
                    Nev = CreateNev.Trim(),
                    Idotartam = CreateIdotartam > 0 ? CreateIdotartam : 30,
                    Ar = CreateAr >= 0 ? CreateAr : 0,
                    Leiras = CreateLeiras?.Trim() ?? "",
                    Sorszam = await _context.Szolgaltatasok.CountAsync() + 1
                };
                _context.Szolgaltatasok.Add(uj);
                await _context.SaveChangesAsync();

                var hibak = new List<string>();

                var sotet = await TrySaveSzolgaltatasKepAsync(uj.Id, "sotet", CreateKepSotet, null);
                if (sotet.Error != null) hibak.Add($"Sötét: {sotet.Error}");
                else if (sotet.FileName != null) uj.KepFajlNeve = sotet.FileName;

                if (hibak.Count > 0)
                    TempData["SzolgKepError"] = string.Join(" ", hibak);

                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/Account/AdminFelulet", new { section = "szolgaltatasok" });
        }

        public async Task<IActionResult> OnPostUpdateAsync(
            int Id,
            string EditNev,
            int EditIdotartam,
            decimal EditAr,
            string EditLeiras,
            int EditSorszam,
            IFormFile? EditKepSotet,
            IFormFile? EditKepVilagos)
        {
            var hibak = new List<string>();
            var s = await _context.Szolgaltatasok.FindAsync(Id);

            if (s != null)
            {
                s.Nev = (EditNev ?? "").Trim();
                s.Idotartam = EditIdotartam > 0 ? EditIdotartam : s.Idotartam;
                s.Ar = EditAr >= 0 ? EditAr : s.Ar;
                s.Leiras = EditLeiras?.Trim() ?? "";
                s.Sorszam = EditSorszam;

                if (EditKepSotet != null && EditKepSotet.Length > 0)
                {
                    var ered = await TrySaveSzolgaltatasKepAsync(s.Id, "sotet", EditKepSotet, s.KepFajlNeve);
                    if (ered.Error != null) hibak.Add($"Sötét: {ered.Error}");
                    else if (ered.FileName != null) s.KepFajlNeve = ered.FileName;
                }

                if (hibak.Count > 0)
                    TempData["SzolgKepError"] = string.Join(" ", hibak);

                await _context.SaveChangesAsync();
            }

            if (hibak.Count > 0)
                return RedirectToPage("/Account/AdminFelulet", new { section = "szolgaltatasok", editId = Id });

            return RedirectToPage("/Account/AdminFelulet", new { section = "szolgaltatasok" });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var s = await _context.Szolgaltatasok.FindAsync(id);
            if (s != null)
            {
                TorolSzolgaltatasKepFajlt(s.KepFajlNeve);
                TorolSzolgaltatasKepFajlt(s.KepFajlNev_Vilagos);
                _context.Szolgaltatasok.Remove(s);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/Account/AdminFelulet", new { section = "szolgaltatasok" });
        }

        // Fodrasz szekció - Meth


        public async Task<IActionResult> OnPostCreateFodraszAsync(int[]? SelectedSzolgaltatasIds)
        {
            if (string.IsNullOrWhiteSpace(CreateFodraszNev))
                return RedirectToPage("/Account/AdminFelulet", new { section = "fodraszok" });

            var f = new Fodrasz
            {
                Nev = CreateFodraszNev.Trim(), 
                Email = CreateFodraszEmail.Trim() ?? "",
                Telefon = CreateFodraszTelefon.Trim() ?? "",
                Specializacio = CreateFodraszSpecializacio.Trim() ?? ""
            };

            var szolgLista = await _context.Szolgaltatasok
                .Where(s => SelectedSzolgaltatasIds != null && SelectedSzolgaltatasIds.Contains(s.Id))
                .ToListAsync();

            foreach (var item in szolgLista)
            {
                f.VallaltSzolgaltatasok.Add(item);
            }
            _context.Fodraszok.Add(f);
            await _context.SaveChangesAsync();
            return RedirectToPage("/Account/AdminFelulet", new { section = "fodraszok" });

        }

        public async Task<IActionResult> OnPostUpdateFodraszAsync(int Id, string EditFodraszNev, string EditFodraszEmail, string EditFodraszTelefon, string EditFodraszSpecializacio, int[]? SelectedSzolgaltatasIds)
        {
            var f = await _context.Fodraszok.Include(x => x.VallaltSzolgaltatasok).FirstOrDefaultAsync(x => x.ID == Id);

            if(f == null)
                return RedirectToPage("/Account/AdminFelulet", new { section = "fodraszok" });

            f.Nev = (EditFodraszNev ?? "").Trim();
            f.Email = (EditFodraszEmail ?? "").Trim();
            f.Telefon=(EditFodraszTelefon ?? "").Trim();
            f.Specializacio = (EditFodraszSpecializacio ?? "").Trim();

            var szolgLista = await _context.Szolgaltatasok
                .Where(s => SelectedSzolgaltatasIds != null && SelectedSzolgaltatasIds.Contains(s.Id))
                .ToListAsync();

            f.VallaltSzolgaltatasok.Clear();
            foreach (var item in szolgLista)
            {
                f.VallaltSzolgaltatasok.Add(item);
            }
            await _context.SaveChangesAsync();
            return RedirectToPage("/Account/AdminFelulet", new { section = "fodraszok" });
        }

        public async Task<IActionResult> OnPostDeleteFodraszAsync(int id)
        {
            var f = await _context.Fodraszok.FindAsync(id);
            if (f != null)
            {
                _context.Fodraszok.Remove(f);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("Account/AdminFelulet", new { section = "fodraszok" });
        }


        // Felhasználók szekció - Meth
        public async Task<IActionResult> OnPostCreateUserAsync(int[]? SelectedSzolgaltatasIds)
        {
            if (string.IsNullOrWhiteSpace(CreateUserEmail) || string.IsNullOrWhiteSpace(CreateUserPassword) || string.IsNullOrWhiteSpace(CreateUserRole))
            {
                TempData["UserError"] = "E-mail, jelszó és jogkör megadása kötelező.";
                return RedirectToPage("/Account/AdminFelulet", new { section = "felhasznalok" });
            }

            Felhasznalo user;

            if (CreateUserRole == "Fodrasz")
            {
                var fodrasz = new Fodrasz
                {
                    Nev = (CreateFodraszNev ?? "").Trim(),
                    Email = CreateUserEmail.Trim(),
                    Telefon = (CreateFodraszTelefon ?? "").Trim(),
                    Specializacio = (CreateFodraszSpecializacio ?? "").Trim()
                };
                var szolgLista = await _context.Szolgaltatasok
                    .Where(s => SelectedSzolgaltatasIds != null && SelectedSzolgaltatasIds.Contains(s.Id))
                    .ToListAsync();
                foreach (var s in szolgLista)
                    fodrasz.VallaltSzolgaltatasok.Add(s);
                _context.Fodraszok.Add(fodrasz);
                await _context.SaveChangesAsync();

                user = new Felhasznalo
                {
                    UserName = CreateUserEmail,
                    Email = CreateUserEmail,
                    EmailConfirmed = true,
                    FodraszId = fodrasz.ID
                };
            }
            else
            {
                user = new Felhasznalo
                {
                    UserName = CreateUserEmail,
                    Email = CreateUserEmail,
                    EmailConfirmed = true,
                    Nev = ""
                };
            }

            var result = await _userManager.CreateAsync(user, CreateUserPassword);

            if (result.Succeeded)
            {
                var roleExists = await _context.Roles.AnyAsync(r => r.Name == CreateUserRole);
                if (roleExists)
                    await _userManager.AddToRoleAsync(user, CreateUserRole);
                else
                    TempData["UserError"] = "A kiválasztott jogkör nem létezik.";
                TempData["UserSuccess"] = "Felhasználó létrehozva.";
            }
            else
            {
                TempData["UserError"] = "Hiba: " + string.Join(" ", result.Errors.Select(e => e.Description));
            }

            return RedirectToPage("/Account/AdminFelulet", new { section = "felhasznalok" });
        }

        public async Task<IActionResult> OnPostSetPasswordAsync(string userId, string NewPassword)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToPage("/Account/AdminFelulet", new { section = "felhasznalok" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["UserError"] = "A felhasználó nem található.";
                return RedirectToPage("/Account/AdminFelulet", new { section = "felhasznalok" });
            }

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                TempData["UserError"] = "Az új jelszó legalább 6 karakter kell legyen.";
                return RedirectToPage("/Account/AdminFelulet", new { section = "felhasznalok", resetPasswordUserId = userId });
            }

            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, NewPassword);

            if (result.Succeeded)
                TempData["UserSuccess"] = "Jelszó módosítva.";
            else
                TempData["UserError"] = string.Join(" ", result.Errors.Select(e => e.Description));

            return RedirectToPage("/Account/AdminFelulet", new { section = "felhasznalok" });
        }

        private async Task<(string? Error, string? FileName)> TrySaveSzolgaltatasKepAsync(int szolgId,string variant,IFormFile? file,string? korabbiFajlNeve)
        {
            if (file == null || file.Length == 0)
                return (null, null);

            if (file.Length > SzolgKepMaxMeretBajt)
                return ("max. 5 MB.", null);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !SzolgKepKiterjesztesek.Contains(ext))
                return ("csak jpg/png/webp/avif.", null);

            var dir = Path.Combine(_env.WebRootPath, "kepek", "szolgaltatasok");
            Directory.CreateDirectory(dir);

            var ujNev = $"szolg-{szolgId}-{variant}-{Guid.NewGuid():N}{ext}";
            var teljesUt = Path.Combine(dir, ujNev);

            try
            {
                await using(var bemenet = file.OpenReadStream())
                    using(var image= await Image.LoadAsync(bemenet))
                    {
                    var encoder = new WebpEncoder { Quality = 82 };
                    await image.SaveAsync(teljesUt,encoder);
                    }
            }
            catch (UnknownImageFormatException)
            {
                return ("nem érvényes a kiválasztott kép", null);
            }
            

            if (!string.IsNullOrWhiteSpace(korabbiFajlNeve))
                TorolSzolgaltatasKepFajlt(korabbiFajlNeve);

            return (null, ujNev);
        }

        private void TorolSzolgaltatasKepFajlt(string? fajlNev)
        {
            if (string.IsNullOrWhiteSpace(fajlNev))
                return;

            var fajlNevNorm = Path.GetFileName(fajlNev);
            if (string.IsNullOrEmpty(fajlNevNorm))
                return;

            var teljes = Path.GetFullPath(Path.Combine(_env.WebRootPath, "kepek", "szolgaltatasok", fajlNevNorm));
            var engedettGyoker = Path.GetFullPath(Path.Combine(_env.WebRootPath, "kepek", "szolgaltatasok"));

            if (!teljes.StartsWith(engedettGyoker, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                if (System.IO.File.Exists(teljes))
                    System.IO.File.Delete(teljes);
            }
            catch { }
        }
    }
}
