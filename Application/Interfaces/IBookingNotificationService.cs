using Application.DTOs;

namespace Application.Interfaces;

public interface IBookingNotificationService
{
    Task SendAsync(BookingMessage message);
}
