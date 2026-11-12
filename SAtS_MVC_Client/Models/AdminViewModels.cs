namespace SAtS_MVC_Client.Models
{
    //------------------------------------------------------------
    // Caleb McIntyre - 12/11/2026
    // ViewModels for the Admin pages
    //------------------------------------------------------------

    //One row in the Admin user list
    public class AdminUserViewModel
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string? FullName { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    //The page for ticking which roles a user has
    public class ManageRolesViewModel
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string? FullName { get; set; }
        public List<RoleSelection> Roles { get; set; } = new List<RoleSelection>();
    }

    //One checkbox on the Manage Roles page
    public class RoleSelection
    {
        public string RoleName { get; set; } = "";
        public bool IsSelected { get; set; }
    }
}
