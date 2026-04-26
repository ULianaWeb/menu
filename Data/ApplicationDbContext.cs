using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using menu.Models;
using menu.Areas.Identity.Data;
using menu.Models;

namespace menu.Data
{
    public class ApplicationDbContext : IdentityDbContext<menuUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Dish> Dishes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Dish>()
                // many-to-many зв'язок між Dish та Ingredient
                .HasMany(d => d.Ingredients)
                .WithMany(i => i.Dishes);
        }
    }
}