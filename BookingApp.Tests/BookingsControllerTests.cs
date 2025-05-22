// BookingApp.Tests/BookingsControllerTests.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BookingApp.Controllers;
using BookingApp.Data;
using BookingApp.DTOs;
using BookingApp.Models;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace BookingApp.Tests
{
    public class BookingsControllerTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            // Use an in-memory database for testing
            // Each test gets a fresh, isolated database context
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name for each test
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact] // XUnit attribute to mark a method as a test
        public async Task Book_ValidRequest_ReturnsOkAndCreatesBooking()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            // Seed initial data for this specific test
            dbContext.Members.Add(new Member { Id = 1, Name = "Test Member", BookingCount = 0, DateJoined = DateTime.UtcNow, Surname = "Test" });
            dbContext.InventoryItems.Add(new InventoryItem { Id = 1, Title = "Test Item", Description = "Desc", RemainingCount = 5, ExpirationDate = DateTime.UtcNow.AddYears(1) });
            await dbContext.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<BookingsController>>(); // Mock the logger
            var controller = new BookingsController(dbContext, mockLogger.Object);

            var request = new BookingRequest { MemberId = 1, InventoryItemId = 1 };

            // Act
            var result = await controller.Book(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result); // Check if the result is an OK response
            Assert.NotNull(okResult.Value);

            // Verify the booking was created in the database
            var booking = await dbContext.Bookings.FirstOrDefaultAsync();
            Assert.NotNull(booking);
            Assert.Equal(1, booking.MemberId);
            Assert.Equal(1, booking.InventoryItemId);
            Assert.False(booking.IsCancelled);

            // Verify counts were updated
            var member = await dbContext.Members.FindAsync(1);
            Assert.Equal(1, member.BookingCount);
            var inventoryItem = await dbContext.InventoryItems.FindAsync(1);
            Assert.Equal(4, inventoryItem.RemainingCount); // Original 5 - 1 booking = 4
        }

        [Fact]
        public async Task Book_MemberNotFound_ReturnsNotFound()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var mockLogger = new Mock<ILogger<BookingsController>>();
            var controller = new BookingsController(dbContext, mockLogger.Object);
            var request = new BookingRequest { MemberId = 99, InventoryItemId = 1 }; // Non-existent member

            // Act
            var result = await controller.Book(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Book_InventoryItemNotFound_ReturnsNotFound()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            dbContext.Members.Add(new Member { Id = 1, Name = "Test Member", BookingCount = 0, DateJoined = DateTime.UtcNow, Surname = "Test" });
            await dbContext.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<BookingsController>>();
            var controller = new BookingsController(dbContext, mockLogger.Object);
            var request = new BookingRequest { MemberId = 1, InventoryItemId = 99 }; // Non-existent item

            // Act
            var result = await controller.Book(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Book_MemberMaxBookingsReached_ReturnsBadRequest()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            // Member with max bookings reached (MAX_BOOKINGS is 2)
            dbContext.Members.Add(new Member { Id = 1, Name = "Test Member", BookingCount = 2, DateJoined = DateTime.UtcNow, Surname = "Test" });
            dbContext.InventoryItems.Add(new InventoryItem { Id = 1, Title = "Test Item", Description = "Desc", RemainingCount = 5, ExpirationDate = DateTime.UtcNow.AddYears(1) });
            await dbContext.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<BookingsController>>();
            var controller = new BookingsController(dbContext, mockLogger.Object);
            var request = new BookingRequest { MemberId = 1, InventoryItemId = 1 };

            // Act
            var result = await controller.Book(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("maximum allowed bookings", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task Book_InventoryDepleted_ReturnsBadRequest()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            dbContext.Members.Add(new Member { Id = 1, Name = "Test Member", BookingCount = 0, DateJoined = DateTime.UtcNow, Surname = "Test" });
            // Item with 0 remaining count
            dbContext.InventoryItems.Add(new InventoryItem { Id = 1, Title = "Test Item", Description = "Desc", RemainingCount = 0, ExpirationDate = DateTime.UtcNow.AddYears(1) });
            await dbContext.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<BookingsController>>();
            var controller = new BookingsController(dbContext, mockLogger.Object);
            var request = new BookingRequest { MemberId = 1, InventoryItemId = 1 };

            // Act
            var result = await controller.Book(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("out of stock", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task Cancel_ValidRequest_ReturnsOkAndCancelsBooking()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            // Seed data: a member, an inventory item, and an existing booking
            var member = new Member { Id = 1, Name = "Test Member", BookingCount = 1, DateJoined = DateTime.UtcNow, Surname = "Test" };
            var inventoryItem = new InventoryItem { Id = 101, Title = "Cancelable Item", Description = "Desc", RemainingCount = 4, ExpirationDate = DateTime.UtcNow.AddYears(1) };
            var existingBooking = new Booking { Id = 1, MemberId = 1, InventoryItemId = 101, BookingDateTime = DateTime.UtcNow.AddDays(-1), IsCancelled = false };

            dbContext.Members.Add(member);
            dbContext.InventoryItems.Add(inventoryItem);
            dbContext.Bookings.Add(existingBooking);
            await dbContext.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<BookingsController>>();
            var controller = new BookingsController(dbContext, mockLogger.Object);
            var request = new CancellationRequest { BookingId = 1 };

            // Act
            var result = await controller.Cancel(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Verify the booking status was updated
            var cancelledBooking = await dbContext.Bookings.FindAsync(1);
            Assert.NotNull(cancelledBooking);
            Assert.True(cancelledBooking.IsCancelled);

            // Verify inventory count was returned
            var updatedInventoryItem = await dbContext.InventoryItems.FindAsync(101);
            Assert.Equal(5, updatedInventoryItem.RemainingCount); // Original 4 + 1 cancelled = 5

            // Verify member booking count was decremented
            var updatedMember = await dbContext.Members.FindAsync(1);
            Assert.Equal(0, updatedMember.BookingCount); // Original 1 - 1 cancelled = 0
        }

        [Fact]
        public async Task Cancel_BookingNotFound_ReturnsNotFound()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var mockLogger = new Mock<ILogger<BookingsController>>();
            var controller = new BookingsController(dbContext, mockLogger.Object);
            var request = new CancellationRequest { BookingId = 999 }; // Non-existent booking

            // Act
            var result = await controller.Cancel(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Cancel_AlreadyCancelledBooking_ReturnsBadRequest()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            // Seed an already cancelled booking
            var member = new Member { Id = 1, Name = "Test Member", BookingCount = 0, DateJoined = DateTime.UtcNow, Surname = "Test" };
            var inventoryItem = new InventoryItem { Id = 101, Title = "Cancelable Item", Description = "Desc", RemainingCount = 5, ExpirationDate = DateTime.UtcNow.AddYears(1) };
            var existingBooking = new Booking { Id = 1, MemberId = 1, InventoryItemId = 101, BookingDateTime = DateTime.UtcNow.AddDays(-1), IsCancelled = true }; // Already cancelled

            dbContext.Members.Add(member);
            dbContext.InventoryItems.Add(inventoryItem);
            dbContext.Bookings.Add(existingBooking);
            await dbContext.SaveChangesAsync();

            var mockLogger = new Mock<ILogger<BookingsController>>();
            var controller = new BookingsController(dbContext, mockLogger.Object);
            var request = new CancellationRequest { BookingId = 1 };

            // Act
            var result = await controller.Cancel(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("already cancelled", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task Cancel_InvalidBookingId_ReturnsBadRequest()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var mockLogger = new Mock<ILogger<BookingsController>>();
            var controller = new BookingsController(dbContext, mockLogger.Object);
            var request = new CancellationRequest { BookingId = 0 }; // Invalid ID

            // Act
            var result = await controller.Cancel(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("BookingId must be a positive integer", badRequestResult.Value.ToString());
        }
    }
}