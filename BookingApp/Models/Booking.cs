// BookingApp/Models/Booking.cs
using System;

namespace BookingApp.Models
{
    public class Booking
    {
        public int Id { get; set; } // Primary Key, will also serve as the booking reference
        public int MemberId { get; set; } // Foreign Key to Member
        public Member Member { get; set; } // Navigation property to the Member

        public int InventoryItemId { get; set; } // Foreign Key to InventoryItem
        public InventoryItem InventoryItem { get; set; } // Navigation property to the InventoryItem

        public DateTime BookingDateTime { get; set; } // Datetime when the booking was made
        public bool IsCancelled { get; set; } = false; // Status of the booking
    }
}