using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using menu.Data;
using menu.Models;
using System.Text.Json;

namespace menu.Controllers
{
    public class DishesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DishesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index()
        {
            return View(await _context.Dishes.Include(d => d.Ingredients).ToListAsync());
        }

        // ================= CREATE =================
        public async Task<IActionResult> Create()
        {
            ViewBag.Ingredients = await _context.Ingredients.ToListAsync();

            // читаємо з сесії
            var json = HttpContext.Session.GetString("DishCreate");
            if (json != null)
            {
                var dish = JsonSerializer.Deserialize<Dish>(json);
                return View(dish);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Dish dish, int[] selectedIngredients)
        {
            // зберігаємо у сесію
            HttpContext.Session.SetString("DishCreate", JsonSerializer.Serialize(dish));

            if (!ModelState.IsValid)
            {
                ViewBag.Ingredients = await _context.Ingredients.ToListAsync();
                return View(dish);
            }

            if (selectedIngredients != null)
            {
                dish.Ingredients = await _context.Ingredients
                    .Where(i => selectedIngredients.Contains(i.Id))
                    .ToListAsync();
            }

            _context.Add(dish);
            await _context.SaveChangesAsync();

            // очистка
            HttpContext.Session.Remove("DishCreate");

            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT =================
        public async Task<IActionResult> Edit(int id)
        {
            var dish = await _context.Dishes
                .Include(d => d.Ingredients)
                .FirstOrDefaultAsync(d => d.Id == id);

            ViewBag.Ingredients = await _context.Ingredients.ToListAsync();

            // перевірка сесії
            var json = HttpContext.Session.GetString("DishEdit");
            if (json != null)
            {
                var sessionDish = JsonSerializer.Deserialize<Dish>(json);
                if (sessionDish.Id == id)
                    return View(sessionDish);
            }

            return View(dish);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Dish dish, int[] selectedIngredients)
        {
            HttpContext.Session.SetString("DishEdit", JsonSerializer.Serialize(dish));

            if (!ModelState.IsValid)
            {
                ViewBag.Ingredients = await _context.Ingredients.ToListAsync();
                return View(dish);
            }

            var dishToUpdate = await _context.Dishes
                .Include(d => d.Ingredients)
                .FirstAsync(d => d.Id == id);

            dishToUpdate.Name = dish.Name;
            dishToUpdate.Description = dish.Description;
            dishToUpdate.Price = dish.Price;
            dishToUpdate.Category = dish.Category;
            dishToUpdate.Calories = dish.Calories;
            dishToUpdate.CookingTimeMinutes = dish.CookingTimeMinutes;

            dishToUpdate.Ingredients.Clear();

            if (selectedIngredients != null)
            {
                var ingredients = await _context.Ingredients
                    .Where(i => selectedIngredients.Contains(i.Id))
                    .ToListAsync();

                foreach (var ing in ingredients)
                    dishToUpdate.Ingredients.Add(ing);
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("DishEdit");

            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        public async Task<IActionResult> Delete(int id)
        {
            var dish = await _context.Dishes.FindAsync(id);
            return View(dish);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dish = await _context.Dishes.FindAsync(id);

            if (dish != null)
            {
                _context.Dishes.Remove(dish);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult CreateDraft([FromBody] Dish dish)
        {
            HttpContext.Session.SetString("DishCreate", JsonSerializer.Serialize(dish));
            return Ok();
        }

        [HttpPost]
        public IActionResult EditDraft([FromBody] Dish dish)
        {
            HttpContext.Session.SetString("DishEdit", JsonSerializer.Serialize(dish));
            return Ok();
        }
    }
}