using Application.ApiResult;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Data;
using Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(int id)
    {
        return await _context.Bookings.Include(b => b.Event).FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<PagedResult<Booking>> GetMyAsync(int userId, BookingStatus? status, int page, int pageSize)
    {
        var query = _context.Bookings
            .AsNoTracking()
            .Include(b => b.Event)
            .Where(b => b.UserId == userId);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Id)
            .ToPagedListAsync(page, pageSize);
    }

    public async Task<List<Booking>> GetExpiredPendingAsync(DateTime now)
    {
        return await _context.Bookings
            .Include(b => b.Event)
            .Where(b => b.Status == BookingStatus.Pending && b.ExpiresAt <= now)
            .ToListAsync();
    }

    public void Add(Booking booking)
    {
        _context.Bookings.Add(booking);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
