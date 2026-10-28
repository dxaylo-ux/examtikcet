using Application.ApiResult;
using Application.Interfaces;
using Domain.Entities;
using Domain.Filters;
using Infrastructure.Persistence.Data;
using Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        return await _context.Events.Include(e => e.Category).FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<PagedResult<Event>> GetPagedAsync(EventFilter filter)
    {
        var now = DateTime.UtcNow;

        var query = _context.Events
            .AsNoTracking()
            .Include(e => e.Category)
            .Where(e => e.StartsAt > now);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(search) || e.Location.ToLower().Contains(search));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(e => e.CategoryId == filter.CategoryId.Value);

        if (filter.MinPrice.HasValue)
            query = query.Where(e => e.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(e => e.Price <= filter.MaxPrice.Value);

        IOrderedQueryable<Event> ordered = filter.SortBy switch
        {
            "price" => filter.Desc ? query.OrderByDescending(e => e.Price) : query.OrderBy(e => e.Price),
            "title" => filter.Desc ? query.OrderByDescending(e => e.Title) : query.OrderBy(e => e.Title),
            _ => filter.Desc ? query.OrderByDescending(e => e.StartsAt) : query.OrderBy(e => e.StartsAt)
        };

        return await ordered.ThenBy(e => e.Id).ToPagedListAsync(filter.Page, filter.PageSize);
    }

    public void Add(Event ev)
    {
        _context.Events.Add(ev);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
