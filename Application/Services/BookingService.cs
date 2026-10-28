using Application.ApiResult;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookings;
    private readonly IEventRepository _events;
    private readonly IMessagePublisher _publisher;
    private readonly IEventCache _cache;
    private readonly BookingSettings _settings;

    public BookingService(IBookingRepository bookings, IEventRepository events, IMessagePublisher publisher,
        IEventCache cache, IOptions<BookingSettings> settings)
    {
        _bookings = bookings;
        _events = events;
        _publisher = publisher;
        _cache = cache;
        _settings = settings.Value;
    }

    public async Task<Result<BookingDto>> CreateAsync(CreateBookingDto dto, int userId)
    {
        if (dto.Seats < 1 || dto.Seats > 10)
            return Result<BookingDto>.Fail("Seats должно быть от 1 до 10", ErrorType.Validation);

        var ev = await _events.GetByIdAsync(dto.EventId);
        if (ev == null)
            return Result<BookingDto>.Fail("Событие не найдено", ErrorType.NotFound);

        if (ev.StartsAt <= DateTime.UtcNow)
            return Result<BookingDto>.Fail("Событие уже началось", ErrorType.Conflict);

        if (ev.OrganizerId == userId)
            return Result<BookingDto>.Fail("Нельзя бронировать своё событие", ErrorType.Forbidden);

        if (ev.AvailableSeats < dto.Seats)
            return Result<BookingDto>.Fail("Недостаточно свободных мест", ErrorType.Conflict);

        var now = DateTime.UtcNow;
        var booking = new Booking
        {
            EventId = ev.Id,
            Event = ev,
            UserId = userId,
            Seats = dto.Seats,
            TotalPrice = ev.Price * dto.Seats,
            Status = BookingStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_settings.HoldMinutes)
        };

        ev.AvailableSeats -= dto.Seats;
        _bookings.Add(booking);
        await _bookings.SaveChangesAsync();

        _cache.Invalidate(ev.Id);
        await _publisher.PublishAsync(new BookingMessage { BookingId = booking.Id, Type = "Created" });

        return Result<BookingDto>.Success(new BookingDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            EventTitle = ev.Title,
            Seats = booking.Seats,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            ExpiresAt = booking.ExpiresAt
        });
    }

    public async Task<Result<PagedResult<BookingDto>>> GetMyAsync(int userId, string? status, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize > 100) pageSize = 100;
        if (pageSize < 10) pageSize = 10;

        BookingStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<BookingStatus>(status.Trim(), true, out var s) || !Enum.IsDefined(s))
                return Result<PagedResult<BookingDto>>.Fail("Неизвестный status", ErrorType.Validation);
            parsedStatus = s;
        }

        var result = await _bookings.GetMyAsync(userId, parsedStatus, page, pageSize);

        return Result<PagedResult<BookingDto>>.Success(new PagedResult<BookingDto>
        {
            Items = result.Items.Select(b => new BookingDto
            {
                Id = b.Id,
                EventId = b.EventId,
                EventTitle = b.Event!.Title,
                Seats = b.Seats,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                CreatedAt = b.CreatedAt,
                ExpiresAt = b.ExpiresAt
            }).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        });
    }

    public async Task<Result<BookingDto>> PayAsync(int id, int userId)
    {
        var booking = await _bookings.GetByIdAsync(id);
        if (booking == null)
            return Result<BookingDto>.Fail("Бронь не найдена", ErrorType.NotFound);

        if (booking.UserId != userId)
            return Result<BookingDto>.Fail("Это не ваша бронь", ErrorType.Forbidden);

        if (booking.Status != BookingStatus.Pending || booking.ExpiresAt <= DateTime.UtcNow)
            return Result<BookingDto>.Fail("Бронь нельзя оплатить", ErrorType.Conflict);

        booking.Status = BookingStatus.Confirmed;
        await _bookings.SaveChangesAsync();

        await _publisher.PublishAsync(new BookingMessage { BookingId = booking.Id, Type = "Confirmed" });

        return Result<BookingDto>.Success(new BookingDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            EventTitle = booking.Event!.Title,
            Seats = booking.Seats,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            ExpiresAt = booking.ExpiresAt
        });
    }

    public async Task<Result<BookingDto>> CancelAsync(int id, int userId, bool isAdmin)
    {
        var booking = await _bookings.GetByIdAsync(id);
        if (booking == null)
            return Result<BookingDto>.Fail("Бронь не найдена", ErrorType.NotFound);

        if (booking.UserId != userId && !isAdmin)
            return Result<BookingDto>.Fail("Это не ваша бронь", ErrorType.Forbidden);

        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
            return Result<BookingDto>.Fail("Бронь нельзя отменить", ErrorType.Conflict);

        booking.Status = BookingStatus.Cancelled;
        booking.Event!.AvailableSeats += booking.Seats;
        await _bookings.SaveChangesAsync();

        _cache.Invalidate(booking.EventId);
        await _publisher.PublishAsync(new BookingMessage { BookingId = booking.Id, Type = "Cancelled" });

        return Result<BookingDto>.Success(new BookingDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            EventTitle = booking.Event.Title,
            Seats = booking.Seats,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status.ToString(),
            CreatedAt = booking.CreatedAt,
            ExpiresAt = booking.ExpiresAt
        });
    }
}
