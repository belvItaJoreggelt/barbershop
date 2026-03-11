using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using static barberShop.Pages.IdopontfoglaloModel;

namespace barberShop.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<Felhasznalo> _signInManager;

        public LoginModel(SignInManager<Felhasznalo> signInManager)
        {
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string Section { get; set; } = "bejelentkezes";
        public class InputModel
        {
            [Required(ErrorMessage = "e-mail megadása kötelező")]
            [EmailAddress]
            public string EmailAddress { get; set; } = string.Empty;

            [Required(ErrorMessage ="jelszó megadása kötelező")]
            [DataType(DataType.Password)]
            public string Password { get; set; }=string.Empty;

            public bool RememberMe { get; set; }
        }
        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var result = await _signInManager.PasswordSignInAsync(
                userName: Input.EmailAddress,
                password: Input.Password,
                isPersistent: Input.RememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                // később majd átirányíthatod /Admin/Index-re
                if (User.IsInRole("Admin"))
                {
                    return RedirectToPage("/Account/AdminFelulet");
                }
                else if(User.IsInRole("Fodrasz"))
                {
                    return RedirectToPage("/Account/FodraszFelulet");
                }
                return RedirectToPage("/Index");
            }

            ModelState.AddModelError(string.Empty, "Hibás e-mail vagy jelszó");
            return Page();
        }

    }
}
