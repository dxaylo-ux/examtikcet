using Application.ApiResult;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int id);
    Task<PagedResult<Booking>> GetMyAsync(int userId, BookingStatus? status, int page, int pageSize);
    Task<List<Booking>> GetExpiredPendingAsync(DateTime now);
    void Add(Booking booking);
    Task SaveChangesAsync();
}
