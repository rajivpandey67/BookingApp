// BookingApp/Services/DataSeeder.cs
using BookingApp.Data;
using BookingApp.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Add this using directive for ILogger
using System.Threading.Tasks; // Add this using directive for Task

namespace BookingApp.Services
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(ApplicationDbContext dbContext, ILogger<DataSeeder> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // Mark as async and return Task
        public async Task SeedDataAsync()
        {
            if (await _dbContext.Members.AnyAsync() || await _dbContext.InventoryItems.AnyAsync())
            {
                _logger.LogInformation("Database already contains data. Skipping CSV import.");
                return;
            }

            _logger.LogInformation("Starting CSV data import...");

            await ImportMembersAsync();
            await ImportInventoryItemsAsync();

            _logger.LogInformation("CSV data import complete.");
        }

        // Mark as async and return Task
        private async Task ImportMembersAsync()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "members.csv");
            if (!File.Exists(filePath))
            {
                _logger.LogError($"Members CSV file not found at: {filePath}");
                return;
            }

            try
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    // FIX 1: Use a 'using alias' to explicitly refer to your MemberMap
                    csv.Context.RegisterClassMap<BookingApp.CsvMappings.MemberMap>(); // Explicitly use your MemberMap
                    var members = csv.GetRecords<Member>().ToList();
                    await _dbContext.Members.AddRangeAsync(members);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation($"Successfully imported {members.Count} members.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing members from CSV.");
            }
        }

        // Mark as async and return Task
        private async Task ImportInventoryItemsAsync()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "inventory.csv");
            if (!File.Exists(filePath))
            {
                _logger.LogError($"Inventory CSV file not found at: {filePath}");
                return;
            }

            try
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    // FIX 1: Use a 'using alias' to explicitly refer to your InventoryItemMap
                    csv.Context.RegisterClassMap<BookingApp.CsvMappings.InventoryItemMap>(); // Explicitly use your InventoryItemMap
                    var inventoryItems = csv.GetRecords<InventoryItem>().ToList();
                    await _dbContext.InventoryItems.AddRangeAsync(inventoryItems);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation($"Successfully imported {inventoryItems.Count} inventory items.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing inventory items from CSV.");
            }
        }
    }
}