using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace barberShop.Pages.Account
{
    [Authorize(Roles ="Fodrasz")]
    public class SikerTorlesModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Felhasznalo> _userManager;

        public SikerTorlesModel(AppDbContext appDb, UserManager<Felhasznalo> userManager)
        {
            _context = appDb;
            _userManager = userManager;
        }

        public ToroltIdopont? Torolt { get; set; }


        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.FodraszId == null)
                return RedirectToPage("/Account/Login");

            Torolt = await _context.ToroltIdopontok
                .Include(i=>i.Szolgaltatas)
                .FirstOrDefaultAsync(i=>i.Id == id && i.FodraszId == user.FodraszId.Value);

            if (Torolt == null)
                return NotFound();

            return Page();
        }
    }
}
