// BookingApp/Data/ApplicationDbContext.cs
using BookingApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define your DbSets, which represent your tables in the database
        public DbSet<Member> Members { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // You can add additional configuration here if needed,
            // e.g., for composite keys, unique constraints, etc.
            // For now, default conventions are sufficient.

            // Example to ensure BookingCount is non-negative and defaults to 0
            modelBuilder.Entity<Member>()
                .Property(m => m.BookingCount)
                .HasDefaultValue(0);

            // Example to ensure RemainingCount is non-negative and defaults to 0
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.RemainingCount)
                .HasDefaultValue(0);

            base.OnModelCreating(modelBuilder);
        }
    }
}