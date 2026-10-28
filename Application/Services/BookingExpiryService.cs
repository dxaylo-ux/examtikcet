using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;

namespace Application.Services;

public class BookingExpiryService : IBookingExpiryService
{
    private readonly IBookingRepository _bookings;
    private readonly IEventCache _cache;
    private readonly IMessagePublisher _publisher;

    public BookingExpiryService(IBookingRepository bookings, IEventCache cache, IMessagePublisher publisher)
    {
        _bookings = bookings;
        _cache = cache;
        _publisher = publisher;
    }

    public async Task ExpireAsync()
    {
        var expired = await _bookings.GetExpiredPendingAsync(DateTime.UtcNow);
        if (expired.Count == 0)
            return;

        foreach (var booking in expired)
        {
            booking.Status = BookingStatus.Expired;
            booking.Event!.AvailableSeats += booking.Seats;
        }

        await _bookings.SaveChangesAsync();

        foreach (var booking in expired)
        {
            _cache.Invalidate(booking.EventId);
            await _publisher.PublishAsync(new BookingMessage { BookingId = booking.Id, Type = "Expired" });
        }
    }
}
