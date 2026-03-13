using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace barberShop.Pages.Account
{
    [AllowAnonymous]
    public class RegistryModel : PageModel
    {
        private readonly UserManager<Felhasznalo> _userManager;

        public RegistryModel(UserManager<Felhasznalo> userManager)
        {
            _userManager = userManager;
        }

        #region RegisztracioProps
        [BindProperty]
        [Required(ErrorMessage ="név megadása kötelező")]
        public string RegNev { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage ="e-mail megadása kötelező")]
        [EmailAddress(ErrorMessage = "valós e-mail megadása kötelező")]
        public string RegEmail { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage ="jelszó nem maradhat üresen")]
        [StringLength(100,MinimumLength =6,ErrorMessage ="legalább 6 karakterből kell állnia")]
        public string RegPassword { get; set; } = "";

        [BindProperty]
        [Phone(ErrorMessage ="")]
        [Required(ErrorMessage ="telefonszám megadása kötelező")]
        public string RegTelo { get; set; } = "";

        #endregion

        
        public async Task<IActionResult> OnPostRegisztracioAsync()
        {
            
            if (!ModelState.IsValid)
                return Page();

            var existingUser = await _userManager.FindByEmailAsync(RegEmail);

            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Megadott e-mail már regisztrálva van");
                return Page();
            }

            var user = new Felhasznalo
            {
                UserName = RegEmail,
                Email = RegEmail,
                EmailConfirmed = true,
                PhoneNumber = RegTelo,
                Nev = RegNev.Trim()
            };

            var result = await _userManager.CreateAsync(user, RegPassword);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return Page();
            }

            await _userManager.AddToRoleAsync(user, "Mugli");
            TempData["SuccessMessage"] = "Sikeres regisztráció! Jelentkezz be.";

            return RedirectToPage("/Account/Login");

        }
    }
}
