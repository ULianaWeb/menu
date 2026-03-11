using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace menu.Models
{
    public class Dish
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Range(0, 10000)]
        public decimal Price { get; set; }

        public int CookingTimeMinutes { get; set; }

        public string Category { get; set; }

        public int Calories { get; set; }

        // Many-to-many relationship
        public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
    }
}