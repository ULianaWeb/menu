using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using menu.Areas.Identity.Data;

namespace menu.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<menuUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<menuUser> userManager,
                               RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // список користувачів
        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // форма зміни ролі
        public async Task<IActionResult> EditRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();

            ViewBag.Roles = roles;
            ViewBag.UserRoles = await _userManager.GetRolesAsync(user);

            return View(user);
        }

        // збереження ролі
        [HttpPost]
        public async Task<IActionResult> EditRole(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);

            var currentRoles = await _userManager.GetRolesAsync(user);

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            return RedirectToAction("Index");
        }
    }
}