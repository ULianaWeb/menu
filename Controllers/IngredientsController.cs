using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using menu.Data;
using menu.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace menu.Controllers
{
    public class IngredientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IngredientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Ingredients.ToListAsync());
        }

        // CREATE
        [Authorize]
        public IActionResult Create()
        {
            var json = HttpContext.Session.GetString("IngredientCreate");
            if (json != null)
            {
                var ingredient = JsonSerializer.Deserialize<Ingredient>(json);
                return View(ingredient);
            }

            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Ingredient ingredient)
        {
            HttpContext.Session.SetString("IngredientCreate", JsonSerializer.Serialize(ingredient));

            if (!ModelState.IsValid)
                return View(ingredient);

            _context.Add(ingredient);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("IngredientCreate");

            return RedirectToAction(nameof(Index));
        }

        // EDIT
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);

            var json = HttpContext.Session.GetString("IngredientEdit");
            if (json != null)
            {
                var sessionIngredient = JsonSerializer.Deserialize<Ingredient>(json);
                if (sessionIngredient.Id == id)
                    return View(sessionIngredient);
            }

            return View(ingredient);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(int id, Ingredient ingredient)
        {
            HttpContext.Session.SetString("IngredientEdit", JsonSerializer.Serialize(ingredient));

            if (!ModelState.IsValid)
                return View(ingredient);

            _context.Update(ingredient);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("IngredientEdit");

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            return View(ingredient);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);

            if (ingredient != null)
            {
                _context.Ingredients.Remove(ingredient);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}