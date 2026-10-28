using Application.ApiResult;
using Application.DTOs;

namespace Application.Interfaces;

public interface IBookingService
{
    Task<Result<BookingDto>> CreateAsync(CreateBookingDto dto, int userId);
    Task<Result<PagedResult<BookingDto>>> GetMyAsync(int userId, string? status, int page, int pageSize);
    Task<Result<BookingDto>> PayAsync(int id, int userId);
    Task<Result<BookingDto>> CancelAsync(int id, int userId, bool isAdmin);
}
