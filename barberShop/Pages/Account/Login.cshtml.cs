using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

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


        #region BejelentkezesProps
        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet =true)]
        public string? Section { get; set; } = "bejelentkezes";
        #endregion

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
                //return RedirectToPage("/Index");
            }
            return Page();
        }



        public async Task<IActionResult> OnPostAsync()
        {
            Section = "bejelentkezes";
            ModelState.Remove("Section");

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
                if (User.IsInRole("Admin"))
                {
                    return RedirectToPage("/Account/AdminFelulet");
                }
                else if(User.IsInRole("Fodrasz"))
                {
                    return RedirectToPage("/Account/FodraszFelulet");
                }
                else if (User.IsInRole("Mugli"))
                {
                    return RedirectToPage("/Account/MugliFelulet");
                }
                return RedirectToPage("/Index");
            }

            ModelState.AddModelError(string.Empty, "Hibás e-mail vagy jelszó");
            return Page();
        }

        
        //vége

    }
}
