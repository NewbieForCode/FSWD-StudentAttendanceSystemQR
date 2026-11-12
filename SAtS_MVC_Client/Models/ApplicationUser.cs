using Microsoft.AspNetCore.Identity;

namespace SAtS_MVC_Client.Models
{
    //------------------------------------------------------------
    // Caleb McIntyre - 12/11/2026
    // The user account used by ASP.NET Identity, with an extra
    // custom field (FullName) that is filled in on the Register page.
    //------------------------------------------------------------
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string? FullName { get; set; }
    }
}
