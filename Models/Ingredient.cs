using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace menu.Models
{
    public class Ingredient
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Unit { get; set; }

        public int Calories { get; set; }

        // Many-to-many
        public ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    }
}