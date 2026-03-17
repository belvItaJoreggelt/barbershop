using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace barberShop.Pages.Account
{
    public class MugliFeluletModel : PageModel
    {
        [BindProperty(SupportsGet =true)]
        public string? Section { get; set; } = "";

        public UserManager<Felhasznalo> _userManager;
        public AppDbContext _context;
        public MugliFeluletModel(UserManager<Felhasznalo> userManager, AppDbContext context)
        {
            _userManager = userManager; 
            _context = context;
        }

        #region AdataimProps
        [BindProperty]
        [EmailAddress]
        [Required(ErrorMessage ="e-mail cím megadása kötelező")]
        public string EditEmail { get; set; } = "";

        [BindProperty]
        public string EditPassword { get; set; } = "";

        [BindProperty]
        public string EditNev { get; set; } = "";

        [BindProperty]
        public string EditTel { get; set; } = "";
        #endregion


        #region Foglalasaim
        [BindProperty]
        public List<Idopont> Mai_Foglalasaim { get; set; } = new();
        [BindProperty]
        public List<Idopont> Egyeb_Foglalasaim { get; set; } = new();
        #endregion





        public async Task OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return;

            if (Section == "adataim")
            {
                EditEmail = user.Email ?? "";
                EditNev = user.Nev ?? "";
                EditTel = user.PhoneNumber ?? "";
            }
            else if (Section == "foglalasaim")
            {
                var todayUtc = DBDataTimeHelper.ToUtc(DateTime.Today);

                var osszes = await _context.Idopontok
                    .Include(f => f.Szolgaltatas)
                    .Include(f=>f.Fodrasz)
                    .Where(f => f.CustomerEmail == user.Email)
                    .OrderByDescending(f=>f.EsedekessegiIdopont)
                    .ToListAsync();

                Mai_Foglalasaim = osszes
                    .Where(f=>f.EsedekessegiIdopont == todayUtc)
                    .ToList();

                Egyeb_Foglalasaim = osszes
                    .Where(f => f.EsedekessegiIdopont != todayUtc)
                    .OrderByDescending(f => f.EsedekessegiIdopont)
                    .ToList();
            }
            
            
        }

        public async Task<IActionResult> OnPostSaveBejelentkezesiAsync()
        {
            var user =await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/MugliFelulet", new { section = "adataim"});

            user.Email = (EditEmail ?? "").Trim();
            user.UserName = user.Email;
            user.NormalizedEmail = _userManager.NormalizeEmail(user.Email);
            user.NormalizedUserName = user.NormalizedEmail;

            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrEmpty(EditPassword) && EditPassword.Length >= 6)
            {
                await _userManager.RemovePasswordAsync(user);
                await _userManager.AddPasswordAsync(user, EditPassword);
            }

            TempData["Success"] = "Bejelentkezési adatok sikeresen frissültek";

            return RedirectToPage("/Account/MugliFelulet", new {section="adataim"});

        }

        public async Task<IActionResult> OnPostSaveEgyebAdataimAsync()
        {
            var user =await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/MugliFelulet", new { section = "adataim" });

            user.Nev = (EditNev ?? "").Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(EditTel) ? null : EditTel.Trim();

            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Az egyéb adatok sikeresen frissültek";
            return RedirectToPage("/Account/MugliFelulet", new { section = "adataim" });
        }
    }
}
