using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using menu.Data;
using Microsoft.AspNetCore.Identity;
using menu.Areas.Identity.Data;
using menu.Models;

namespace menu.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<menuUser> _userManager;

        public ChatController(ApplicationDbContext db, IWebHostEnvironment env, UserManager<menuUser> userManager)
        {
            _db = db;
            _env = env;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile()
        {
            var files = Request.Form.Files;
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var file = files[0];
            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            // Оригінальне ім'я для відображення
            var originalFileName = Path.GetFileName(file.FileName);

            // Унікальне ім'я (використовуємо GUID)
            var uniqueName = $"{Guid.NewGuid().ToString()}_{originalFileName}";

            // САНІТИЗАЦІЯ: замінюємо небезпечні символи у імені файлу для збереження на диску
            // Залишаємо букви, цифри, дефіс, підкреслення, крапку і пробіли (за потреби можна прибрати пробіли)
            var safeFileName = System.Text.RegularExpressions.Regex.Replace(uniqueName, @"[^\w\-. ]", "_");

            var filePath = Path.Combine(uploads, safeFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Для URL краще використовувати кодування (щоб уникнути проблем з пробілами, # тощо)
            var encodedFileNameForUrl = Uri.EscapeDataString(safeFileName);
            var fileUrl = $"/uploads/{encodedFileNameForUrl}";

            return Ok(new { fileName = originalFileName, fileUrl });
        }

        [HttpGet]
        public async Task<IActionResult> UsersList()
        {
            var users = _userManager.Users.ToList();
            var result = new System.Collections.Generic.List<object>();
            foreach (var u in users)
            {
                var isAdmin = await _userManager.IsInRoleAsync(u, "Administrator");
                result.Add(new { u.Id, u.Email, IsAdmin = isAdmin });
            }
            return Json(result);
        }
    }
}
