using Microsoft.AspNetCore.Identity;

namespace barberShop
{
    public class Felhasznalo : IdentityUser
    {
        /// <summary>
        /// Ha az admin egyben fodrász is, ide kötődik a Fodrasz profil.
        /// </summary>
        public int? FodraszId { get; set; }
        public string? Nev { get; set; }        
    }
}
