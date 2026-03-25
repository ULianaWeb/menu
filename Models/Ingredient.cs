using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace menu.Models
{
    public class Ingredient
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        public string Unit { get; set; }

        [Required]
        [Range(5, 5000, ErrorMessage = "Calories must be >= 5")]
        public int Calories { get; set; }

        // Many-to-many
        public ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    }
}