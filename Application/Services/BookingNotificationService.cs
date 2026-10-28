using Application.DTOs;
using Application.Identity;
using Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class BookingNotificationService : IBookingNotificationService
{
    private readonly IBookingRepository _bookings;
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _email;

    public BookingNotificationService(IBookingRepository bookings, UserManager<User> userManager, IEmailService email)
    {
        _bookings = bookings;
        _userManager = userManager;
        _email = email;
    }

    public async Task SendAsync(BookingMessage message)
    {
        var booking = await _bookings.GetByIdAsync(message.BookingId)
            ?? throw new InvalidOperationException($"Бронь {message.BookingId} не найдена");

        var user = await _userManager.FindByIdAsync(booking.UserId.ToString())
            ?? throw new InvalidOperationException($"Пользователь {booking.UserId} не найден");

        var details = $"Событие: {booking.Event!.Title}\nМест: {booking.Seats}\nСумма: {booking.TotalPrice}";

        var (subject, body) = message.Type switch
        {
            "Created" => ("Бронь создана", $"Бронь создана. Оплатите до {booking.ExpiresAt:yyyy-MM-dd HH:mm} UTC.\n{details}"),
            "Confirmed" => ("Бронь оплачена", $"Бронь оплачена.\n{details}"),
            "Cancelled" => ("Бронь отменена", $"Бронь отменена.\n{details}"),
            "Expired" => ("Время удержания брони вышло", $"Время удержания брони вышло.\n{details}"),
            _ => throw new InvalidOperationException($"Неизвестный тип сообщения {message.Type}")
        };

        await _email.SendAsync(user.Email!, subject, body);
    }
}
