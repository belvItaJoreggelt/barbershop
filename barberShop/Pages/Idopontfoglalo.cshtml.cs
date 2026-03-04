using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace barberShop.Pages
{
    public class IdopontfoglaloModel : PageModel
    {
        private readonly AppDbContext _context;

        public IdopontfoglaloModel(AppDbContext context)
        {
            _context = context;
        }

        public Fodrasz Fodrasz { get; set; } = null!;
        public Szolgaltatas Szolgaltatas { get; set; } = null!;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int FodraszId { get; set; }
            public int SzolgaltatasId { get; set; }
            public DateTime FoglalasDatum { get; set; } = DateTime.Today;
            public string FoglalasIdo { get; set; } = "10:00";
            public string UgyfelNev { get; set; } = "";
            public string UgyfelEmail { get; set; } = "";
            public string UgyfelTelefon { get; set; } = "";
            public string? Megjegyzes { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? fodraszId, int? szolgaltatasId)
        {
            if (fodraszId == null || szolgaltatasId == null)
                return RedirectToPage("/Index");

            var fodrasz = await _context.Fodraszok.FindAsync(fodraszId);
            var szolg = await _context.Szolgaltatasok.FindAsync(szolgaltatasId);
            if (fodrasz == null || szolg == null)
                return RedirectToPage("/Index");

            Fodrasz = fodrasz;
            Szolgaltatas = szolg;
            Input.FodraszId = fodraszId.Value;
            Input.SzolgaltatasId = szolgaltatasId.Value;
            Input.FoglalasDatum = DateTime.Today;
            Input.FoglalasIdo = "10:00";
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var fodrasz = await _context.Fodraszok.FindAsync(Input.FodraszId);
            var szolg = await _context.Szolgaltatasok.FindAsync(Input.SzolgaltatasId);
            if (fodrasz == null || szolg == null)
                return RedirectToPage("/Index");

            Fodrasz = fodrasz;
            Szolgaltatas = szolg;

            if (string.IsNullOrWhiteSpace(Input.UgyfelNev) || string.IsNullOrWhiteSpace(Input.UgyfelEmail) || string.IsNullOrWhiteSpace(Input.UgyfelTelefon))
            {
                ModelState.AddModelError("", "Név, e-mail és telefon megadása kötelez?.");
                return Page();
            }

            if (!DateTime.TryParse(Input.FoglalasIdo, out var idoResz))
            {
                ModelState.AddModelError("", "Érvényes id?t adjon meg (pl. 10:00).");
                return Page();
            }

            var idopont = new DateTime(
                Input.FoglalasDatum.Year, Input.FoglalasDatum.Month, Input.FoglalasDatum.Day,
                idoResz.Hour, idoResz.Minute, 0);

            if (idopont < DateTime.Now)
            {
                ModelState.AddModelError("", "A foglalás id?pontja nem lehet a múltban.");
                return Page();
            }

            var ujIdopont = new Idopont
            {
                FodraszId = Input.FodraszId,
                SzolgaltatasId = Input.SzolgaltatasId,
                EsedekessegiIdopont = idopont,
                CustomerNeve = Input.UgyfelNev.Trim(),
                CustomerEmail = Input.UgyfelEmail.Trim(),
                CustomerPhone = Input.UgyfelTelefon.Trim(),
                CustomerNotes = Input.Megjegyzes?.Trim()
            };

            _context.Idopontok.Add(ujIdopont);
            await _context.SaveChangesAsync();

            return RedirectToPage("/IdopontSiker", new { idopontId = ujIdopont.ID });
        }
    }
}
