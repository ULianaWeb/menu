using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace menu.Models
{
    public class Dish
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Range(10, 10000, ErrorMessage = "Price must be >= 10")]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 500, ErrorMessage = "Cooking time must be > 0")]
        public int CookingTimeMinutes { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; }

        [Range(5, 5000, ErrorMessage = "Calories must be >= 5")]
        public int Calories { get; set; }

        // Many-to-many relationship
        public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
    }
}