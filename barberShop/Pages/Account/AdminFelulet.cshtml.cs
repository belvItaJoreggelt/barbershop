using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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

        public AdminFeluletModel(AppDbContext context, UserManager<Felhasznalo> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
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
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!string.IsNullOrWhiteSpace(CreateNev))
            {
                _context.Szolgaltatasok.Add(new Szolgaltatas
                {
                    Nev = CreateNev.Trim(),
                    Idotartam = CreateIdotartam > 0 ? CreateIdotartam : 30,
                    Ar = CreateAr >= 0 ? CreateAr : 0,
                    Leiras = CreateLeiras?.Trim() ?? "",
                    Sorszam= await _context.Szolgaltatasok.CountAsync() + 1
                });
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/Account/AdminFelulet", new { section ="szolgaltatasok"});
        }

        /// <summary>POST Update: meglévő szolgáltatás frissítése (Id + mezők), majd vissza az oldalra.</summary>
        public async Task<IActionResult> OnPostUpdateAsync(int Id, string EditNev, int EditIdotartam, decimal EditAr, string EditLeiras, int EditSorszam)
        {
            var s = await _context.Szolgaltatasok.FindAsync(Id);
            if (s != null)
            {
                s.Nev = (EditNev ?? "").Trim();
                s.Idotartam = EditIdotartam > 0 ? EditIdotartam : s.Idotartam;
                s.Ar = EditAr >= 0 ? EditAr : s.Ar;
                s.Leiras = EditLeiras?.Trim() ?? "";
                s.Sorszam = EditSorszam;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/Account/AdminFelulet", new { section = "szolgaltatasok" });
        }

        /// <summary>POST Delete: szolgáltatás törlése id alapján, majd vissza az oldalra.</summary>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var s = await _context.Szolgaltatasok.FindAsync(id);
            if (s != null)
            {
                _context.Szolgaltatasok.Remove(s);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/Account/AdminFelulet", new { section ="szolgaltatasok"});
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
    }
}
