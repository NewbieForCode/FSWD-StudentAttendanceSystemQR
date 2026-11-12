using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAtS_MVC_Client.Models;

namespace SAtS_MVC_Client.Controllers
{
    //------------------------------------------------------------
    // Caleb McIntyre - 12/11/2026
    // Admin area: only Admins can see these pages.
    // Lets an Admin view all user accounts, change their roles,
    // and delete accounts.
    //------------------------------------------------------------
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        //GET: /Admin
        //List every user account and the roles they have
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var model = new List<AdminUserViewModel>();
            foreach (var user in users)
            {
                model.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? "",
                    FullName = user.FullName,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList()
                });
            }

            return View(model);
        }

        //GET: /Admin/ManageRoles/{id}
        //Show a checkbox for every role, ticked if the user has it
        public async Task<IActionResult> ManageRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roleNames = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => r.Name!)
                .ToListAsync();

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new ManageRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? user.UserName ?? "",
                FullName = user.FullName,
                Roles = roleNames.Select(name => new RoleSelection
                {
                    RoleName = name,
                    IsSelected = userRoles.Contains(name)
                }).ToList()
            };

            return View(model);
        }

        //POST: /Admin/ManageRoles
        //Save the ticked roles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            //Stop an Admin from removing their own Admin role and locking themselves out
            var currentUserId = _userManager.GetUserId(User);
            var keepsAdmin = model.Roles.Any(r => r.RoleName == "Admin" && r.IsSelected);
            if (user.Id == currentUserId && !keepsAdmin)
            {
                ModelState.AddModelError(string.Empty, "You can't remove the Admin role from your own account.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList();

            //Remove roles that were unticked
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    AddErrors(removeResult);
                    return View(model);
                }
            }

            //Add roles that were ticked
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    AddErrors(addResult);
                    return View(model);
                }
            }

            TempData["Message"] = $"Roles updated for {model.Email}.";
            return RedirectToAction(nameof(Index));
        }

        //GET: /Admin/Delete/{id}
        //Ask the Admin to confirm before deleting an account
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new AdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? "",
                FullName = user.FullName,
                EmailConfirmed = user.EmailConfirmed,
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            };

            return View(model);
        }

        //POST: /Admin/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            //An Admin can't delete their own account from here
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "You can't delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Message"] = $"Deleted the account {user.Email}.";
            }
            else
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
