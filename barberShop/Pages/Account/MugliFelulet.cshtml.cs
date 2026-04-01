using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;

namespace barberShop.Pages.Account
{
    public class MugliFeluletModel : PageModel
    {
        [BindProperty(SupportsGet =true)]
        public string? Section { get; set; } = "";

        public UserManager<Felhasznalo> _userManager;
        public AppDbContext _context;
        public readonly IEmailKuldo _emailKuldo;
        public MugliFeluletModel(UserManager<Felhasznalo> userManager, AppDbContext context, IEmailKuldo emailKuldo)
        {
            _userManager = userManager; 
            _context = context;
            _emailKuldo = emailKuldo;
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
                var maBudapest = BudapestTime.TodayBudapestDate;

                var maiNapKezdetBudapest = DateTime.SpecifyKind(maBudapest.Date, DateTimeKind.Unspecified);
                var holnapNapKezdetBudapest = maiNapKezdetBudapest.AddDays(1);

                DateTime maiNapKezdetUtc;
                DateTime holnapNapKezdetUtc;

                try
                {
                    maiNapKezdetUtc = BudapestTime.BudapestLocalToUtc(maiNapKezdetBudapest);
                    holnapNapKezdetUtc = BudapestTime.BudapestLocalToUtc(holnapNapKezdetBudapest);
                }
                catch (ArgumentException)
                {
                    Mai_Foglalasaim = new List<Idopont>();
                    Egyeb_Foglalasaim = new List<Idopont>();
                    return;
                }

                var osszes = await _context.Idopontok
                    .Include(f => f.Szolgaltatas)
                    .Include(f => f.Fodrasz)
                    .Where(f => f.CustomerEmail == user.Email)
                    .OrderByDescending(f => f.EsedekessegiIdopont)
                    .ToListAsync();

                Mai_Foglalasaim = osszes
                    .Where(f =>
                        f.EsedekessegiIdopont >= maiNapKezdetUtc &&
                        f.EsedekessegiIdopont < holnapNapKezdetUtc)
                    .ToList();

                Egyeb_Foglalasaim = osszes
                    .Where(f =>
                        f.EsedekessegiIdopont < maiNapKezdetUtc ||
                        f.EsedekessegiIdopont >= holnapNapKezdetUtc)
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

        public async Task<IActionResult> OnPostTorolFoglalasAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.Id == null)
                return RedirectToPage("/Account/MugliFelulet", new { section = "foglalasaim" });

            var idopont = await _context.Idopontok
                .Include(i => i.Szolgaltatas)
                .FirstOrDefaultAsync(i => i.ID == id);

            if (idopont == null)
                return RedirectToPage("/Account/MugliFelulet", new { section = "foglalasaim" });

            

            var mostUtc = DateTime.UtcNow;
            if (idopont.EsedekessegiIdopont > mostUtc && idopont.EsedekessegiIdopont <= mostUtc.AddHours(48))
            {
                TempData["Error"] = "Az időpont kezdete előtt 48 órán belül már nem törölhető az időpont!";
                return RedirectToPage("/Account/MugliFelulet", new { section = "foglalasaim" });
            }


            var archiv = new ToroltIdopont
            {
                EredetiIdopontId = idopont.ID,
                FodraszId = idopont.FodraszId,
                SzolgaltatasId = idopont.SzolgaltatasId,
                EsedekessegiIdopont = idopont.EsedekessegiIdopont,
                FoglalasiIdopont = idopont.FoglalasiIdopont,
                CustomerNeve = idopont.CustomerNeve,
                CustomerEmail = idopont.CustomerEmail,
                CustomerPhone = idopont.CustomerPhone,
                CustomerNotes = idopont.CustomerNotes,
                TorolveUtc = DateTime.UtcNow,
                KiTorolte = user.Email ?? string.Empty
            };

            _context.ToroltIdopontok.Add(archiv);
            _context.Remove(idopont);
            await _context.SaveChangesAsync();

            var subject = "BestBarberShop - foglalás törölve";

            var hu = CultureInfo.GetCultureInfo("hu-HU");
            var toroltIdopontBudapest = BudapestTime.UtcToBudapest(idopont.EsedekessegiIdopont);
            var toroltIdopontSzoveg = toroltIdopontBudapest.ToString("MMMM d. (dddd) HH:mm", hu);

            var szolgaltatas = WebUtility.HtmlEncode(idopont.Szolgaltatas?.Nev ?? "");
            var nev = WebUtility.HtmlEncode(idopont.CustomerNeve ?? "");

            var ujIdopontUrl = "https://bestbarbershopbookingsystem-djf9c0hch0dqexdt.westeurope-01.azurewebsites.net/";
            var maps = "https://www.bing.com/maps/search?mepi=72%7ELocal%7EEmbedded%7EEntity_Vertical_List_Card&ty=17&poicount=18&sei=0&FORM=MPSRPL&q=kelenf%C3%B6ld+fodr%C3%A1szat&secq=%C3%9Ajhull%C3%A1m+Fodr%C3%A1szat+kelenfoeld+fodraszat&sece=ypid.YN8081x11846474530400285953&ppois=47.467506408691406_19.035743713378906_%C3%9Ajhull%C3%A1m+Fodr%C3%A1szat_YN8081x11846474530400285953%7E47.46304702758789_19.034894943237305_X%C3%A9nia+Fodr%C3%A1szat_YN8081x14308692530027957564%7E47.46721649169922_19.042898178100586_B%C3%A1rtfai+Sz%C3%A9ps%C3%A9gszalon+most_YN8081x3342422111653719704%7E&segment=Local&cp=47.467179%7E19.036090&lvl=17.7&style=r";

            var body = $@"
<html lang=""hu"">
<head>
    <meta charset=""utf-8"" />
    <style type=""text/css"">
        body {{ font-family: Arial, Helvetica, sans-serif; color: #333; }}
        h2 {{ color: rgba(191, 162, 122, 0.7); }}
    </style>
</head>
<body>
<div style=""text-align: center;"">
    <h2>Kedves {nev}!</h2>
    <p>Foglalásod törlésre került!</p>
    <p>Részletek:</p>
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""margin: 0 auto; max-width: 320px;"">
        <tr>
            <td style=""padding: 10px 18px; text-align: center; border-radius: 15px; background-color: #e8dcc8; background-image: linear-gradient(to top right, rgba(191,162,122,0.7), #ffffff);"">
                {szolgaltatas}<br />
                {toroltIdopontSzoveg}
            </td>
        </tr>
    </table>
    <p style=""padding-top: 10px;"">
        <a href=""{ujIdopontUrl}"" style=""text-decoration: none; color: white;"">
            <span style=""background:rgba(191, 162, 122, 0.7); display:inline-block; margin: 0 auto; padding: 7px 14px; border-radius: 10px;"">
                új foglalás
            </span>
        </a>
    </p>
    <p style=""padding-top: 20px;"">
        BestBarbershop<br />
        <a href=""{maps}"" style=""color:black;"">1115 Budapest Bártfai utca 38</a><br />
        <a href=""mailto:szaszakpepe@gmail.com"" style=""text-decoration:none; color:black;"">szaszakpepe@gmail.com</a><br />
        <a href=""tel:+36307271232"" style=""color:black;"">+36 30 727 1232</a>
    </p>
</div>
</body>
</html>";

            await _emailKuldo.SendAsync(idopont.CustomerEmail, subject, body);

            return RedirectToPage("/Account/MugliFelulet", new { section = "foglalasaim" });
        }
    }
}
