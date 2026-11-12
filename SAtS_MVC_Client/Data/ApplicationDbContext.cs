using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SAtS_MVC_Client.Models;

// Caleb McIntyre - 12/11/2026
// Changed to use ApplicationUser so the custom FullName field is saved in AspNetUsers.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options)
    {

    }
}
