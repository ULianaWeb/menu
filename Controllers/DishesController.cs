using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using menu.Data;
using menu.Models;

namespace menu.Controllers
{
    public class DishesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DishesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dishes = await _context.Dishes
                .Include(d => d.Ingredients)
                .ToListAsync();

            return View(dishes);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Ingredients = await _context.Ingredients.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Dish dish, int[] selectedIngredients)
        {
            if (selectedIngredients != null)
            {
                dish.Ingredients = await _context.Ingredients
                    .Where(i => selectedIngredients.Contains(i.Id))
                    .ToListAsync();
            }

            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var dish = await _context.Dishes
                .Include(d => d.Ingredients)
                .FirstOrDefaultAsync(d => d.Id == id);

            ViewBag.Ingredients = await _context.Ingredients.ToListAsync();

            return View(dish);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Dish dish, int[] selectedIngredients)
        {
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
                {
                    dishToUpdate.Ingredients.Add(ing);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

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
    }
}