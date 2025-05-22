using BookingApp.Data;
using BookingApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks; // <-- Add this using directive

namespace BookingApp
{
    public class Program
    {
        // CHANGE THIS LINE: Add 'async' and change return type to 'Task'
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            // Configure DbContext with SQLite
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
            // Register the DataSeeder service
            builder.Services.AddScoped<DataSeeder>();
            var app = builder.Build();
            // --- Data Seeder Execution ---
            // This part needs to run asynchronously and is typically done after app.Build()
            // and before app.Run().
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var seeder = services.GetRequiredService<DataSeeder>();
                    await seeder.SeedDataAsync(); // This 'await' is now valid
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }
            // --- End of data seeder section ---
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}