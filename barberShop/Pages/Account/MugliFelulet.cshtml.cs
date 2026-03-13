using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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







        public async Task OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return;

            EditEmail = user.Email ?? "";
            EditNev = user.Nev ?? "";
            EditTel = user.PhoneNumber ?? "";
            
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
