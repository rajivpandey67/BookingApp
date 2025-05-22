// BookingApp/Controllers/BookingsController.cs
using BookingApp.Data;
using BookingApp.DTOs;
using BookingApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BookingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingsController> _logger;

        private const int MAX_BOOKINGS = 2;

        public BookingsController(ApplicationDbContext context, ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Endpoint for booking an item
        // POST: api/Bookings/book
        [HttpPost("book")]
        public async Task<IActionResult> Book([FromBody] BookingRequest request)
        {
            if (request.MemberId <= 0 || request.InventoryItemId <= 0)
            {
                _logger.LogWarning("Invalid booking request: MemberId or InventoryItemId is invalid.");
                return BadRequest("MemberId and InventoryItemId must be positive integers.");
            }

            var member = await _context.Members.FindAsync(request.MemberId);
            if (member == null)
            {
                _logger.LogWarning($"Booking failed: Member with ID {request.MemberId} not found.");
                return NotFound($"Member with ID {request.MemberId} not found.");
            }

            var inventoryItem = await _context.InventoryItems.FindAsync(request.InventoryItemId);
            if (inventoryItem == null)
            {
                _logger.LogWarning($"Booking failed: Inventory item with ID {request.InventoryItemId} not found.");
                return NotFound($"Inventory item with ID {request.InventoryItemId} not found.");
            }

            if (member.BookingCount >= MAX_BOOKINGS)
            {
                _logger.LogWarning($"Booking failed for Member ID {request.MemberId}: Max bookings reached ({MAX_BOOKINGS}).");
                return BadRequest($"Member has reached the maximum allowed bookings of {MAX_BOOKINGS}.");
            }

            if (inventoryItem.RemainingCount <= 0)
            {
                _logger.LogWarning($"Booking failed for Inventory ID {request.InventoryItemId}: Item out of stock.");
                return BadRequest("Inventory item is out of stock.");
            }

            var newBooking = new Booking
            {
                MemberId = request.MemberId,
                InventoryItemId = request.InventoryItemId,
                BookingDateTime = DateTime.UtcNow,
                IsCancelled = false
            };

            inventoryItem.RemainingCount--;
            member.BookingCount++;

            _context.Bookings.Add(newBooking);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Booking successful: Member ID {request.MemberId} booked Inventory Item ID {request.InventoryItemId}. Booking ID: {newBooking.Id}");

            return Ok(new { Message = "Booking successful", BookingId = newBooking.Id, BookingDateTime = newBooking.BookingDateTime });
        }


        // Endpoint for cancelling a booking
        // POST: api/Bookings/cancel
        [HttpPost("cancel")] // This makes the full route /api/Bookings/cancel
        public async Task<IActionResult> Cancel([FromBody] CancellationRequest request)
        {
            // 1. Validate request data
            if (request.BookingId <= 0)
            {
                _logger.LogWarning("Invalid cancellation request: BookingId is invalid.");
                return BadRequest("BookingId must be a positive integer.");
            }

            // 2. Find the booking, including associated Member and InventoryItem
            // We use .Include() to eagerly load related entities needed for updates
            var booking = await _context.Bookings
                                      .Include(b => b.Member)
                                      .Include(b => b.InventoryItem)
                                      .FirstOrDefaultAsync(b => b.Id == request.BookingId);

            if (booking == null)
            {
                _logger.LogWarning($"Cancellation failed: Booking with ID {request.BookingId} not found.");
                return NotFound($"Booking with ID {request.BookingId} not found.");
            }

            // 3. Check if the booking is already cancelled
            if (booking.IsCancelled)
            {
                _logger.LogWarning($"Cancellation failed for Booking ID {request.BookingId}: Already cancelled.");
                return BadRequest($"Booking with ID {request.BookingId} is already cancelled.");
            }

            // 4. Update booking status and related counts
            booking.IsCancelled = true;

            // Return item to inventory
            if (booking.InventoryItem != null)
            {
                booking.InventoryItem.RemainingCount++;
            }
            else
            {
                _logger.LogError($"Inventory item for Booking ID {request.BookingId} was null during cancellation. Data inconsistency suspected.");
            }

            // Free up member's booking slot, ensuring it doesn't go below zero
            if (booking.Member != null)
            {
                if (booking.Member.BookingCount > 0)
                {
                    booking.Member.BookingCount--;
                }
                else
                {
                    _logger.LogWarning($"Member BookingCount for Booking ID {request.BookingId} was already 0 during cancellation. This might indicate previous data corruption or an edge case.");
                }
            }
            else
            {
                _logger.LogError($"Member for Booking ID {request.BookingId} was null during cancellation. Data inconsistency suspected.");
            }

            // 5. Save changes to database
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Booking ID {request.BookingId} successfully cancelled. Member ID: {booking.MemberId}, Inventory Item ID: {booking.InventoryItemId}");

            return Ok(new { Message = "Booking cancelled successfully", BookingId = booking.Id });
        }
    }
}