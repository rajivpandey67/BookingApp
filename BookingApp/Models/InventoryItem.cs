// BookingApp/Models/InventoryItem.cs
using System;
using System.Collections.Generic;

namespace BookingApp.Models
{
    public class InventoryItem
    {
        public int Id { get; set; } // Primary Key for the InventoryItem table
        public string Title { get; set; }
        public string Description { get; set; }
        public int RemainingCount { get; set; } // Remaining stock
        public DateTime ExpirationDate { get; set; }

        // Navigation property for bookings associated with this inventory item
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}