using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;

namespace barberShop.Pages
{
    public class PrivacyModel : PageModel
    {

        [BindProperty(SupportsGet =true)]
        public string Section { get; set; } = "";

        [BindProperty(SupportsGet=true)]
        public int? FodraszId { get; set; }

        [BindProperty(SupportsGet =true)]
        public int? SzolgaltatasId { get; set; }

        [BindProperty(SupportsGet =true)]
        public string? FoglalasDatum { get; set; }

        [BindProperty(SupportsGet =true)]
        public string? FoglalasIdo { get; set; }







        public void OnGet()
        {
        }
    }

}
