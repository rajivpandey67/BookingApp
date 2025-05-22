// BookingApp/Models/Member.cs
using System;
using System.Collections.Generic;

namespace BookingApp.Models
{
    public class Member
    {
        public int Id { get; set; } // Primary Key for the Member table
        public string Name { get; set; }
        public string Surname { get; set; }
        public int BookingCount { get; set; } // Tracks current bookings by the member
        public DateTime DateJoined { get; set; }

        // Navigation property for bookings made by this member
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}