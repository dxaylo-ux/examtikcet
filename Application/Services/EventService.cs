using Application.ApiResult;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Filters;

namespace Application.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _events;
    private readonly ICategoryRepository _categories;
    private readonly IEventCache _cache;

    public EventService(IEventRepository events, ICategoryRepository categories, IEventCache cache)
    {
        _events = events;
        _categories = categories;
        _cache = cache;
    }

    public async Task<Result<PagedResult<EventDto>>> GetListAsync(EventFilter filter)
    {
        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize > 100) filter.PageSize = 100;
        if (filter.PageSize < 10) filter.PageSize = 10;

        filter.SortBy = string.IsNullOrWhiteSpace(filter.SortBy) ? "date" : filter.SortBy.Trim().ToLowerInvariant();
        if (filter.SortBy != "date" && filter.SortBy != "price" && filter.SortBy != "title")
            return Result<PagedResult<EventDto>>.Fail("sortBy должен быть date, price или title", ErrorType.Validation);

        if (filter.MinPrice.HasValue && filter.MaxPrice.HasValue && filter.MinPrice > filter.MaxPrice)
            return Result<PagedResult<EventDto>>.Fail("minPrice не может быть больше maxPrice", ErrorType.Validation);

        var key = _cache.BuildListKey(filter);
        var cached = _cache.GetList(key);
        if (cached != null)
            return Result<PagedResult<EventDto>>.Success(cached);

        var page = await _events.GetPagedAsync(filter);

        var result = new PagedResult<EventDto>
        {
            Items = page.Items.Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Location = e.Location,
                StartsAt = e.StartsAt,
                Price = e.Price,
                TotalSeats = e.TotalSeats,
                AvailableSeats = e.AvailableSeats,
                CreatedAt = e.CreatedAt,
                CategoryId = e.CategoryId,
                CategoryName = e.Category!.Name,
                OrganizerId = e.OrganizerId
            }).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };

        _cache.SetList(key, result);
        return Result<PagedResult<EventDto>>.Success(result);
    }

    public async Task<Result<EventDto>> GetByIdAsync(int id)
    {
        var cached = _cache.GetEvent(id);
        if (cached != null)
            return Result<EventDto>.Success(cached);

        var e = await _events.GetByIdAsync(id);
        if (e == null)
            return Result<EventDto>.Fail("Событие не найдено", ErrorType.NotFound);

        var dto = new EventDto
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            Location = e.Location,
            StartsAt = e.StartsAt,
            Price = e.Price,
            TotalSeats = e.TotalSeats,
            AvailableSeats = e.AvailableSeats,
            CreatedAt = e.CreatedAt,
            CategoryId = e.CategoryId,
            CategoryName = e.Category!.Name,
            OrganizerId = e.OrganizerId
        };

        _cache.SetEvent(id, dto);
        return Result<EventDto>.Success(dto);
    }

    public async Task<Result<EventDto>> CreateAsync(CreateEventDto dto, int organizerId)
    {
        var error = Validate(dto.Title, dto.Description, dto.Location, dto.StartsAt, dto.Price, dto.TotalSeats);
        if (error != null)
            return Result<EventDto>.Fail(error, ErrorType.Validation);

        var category = await _categories.GetByIdAsync(dto.CategoryId);
        if (category == null)
            return Result<EventDto>.Fail("Категория не найдена", ErrorType.NotFound);

        var e = new Event
        {
            Title = dto.Title!.Trim(),
            Description = dto.Description?.Trim(),
            Location = dto.Location!.Trim(),
            StartsAt = dto.StartsAt,
            Price = dto.Price,
            TotalSeats = dto.TotalSeats,
            AvailableSeats = dto.TotalSeats,
            CreatedAt = DateTime.UtcNow,
            CategoryId = category.Id,
            OrganizerId = organizerId
        };

        _events.Add(e);
        await _events.SaveChangesAsync();
        _cache.Invalidate(e.Id);

        return Result<EventDto>.Success(new EventDto
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            Location = e.Location,
            StartsAt = e.StartsAt,
            Price = e.Price,
            TotalSeats = e.TotalSeats,
            AvailableSeats = e.AvailableSeats,
            CreatedAt = e.CreatedAt,
            CategoryId = e.CategoryId,
            CategoryName = category.Name,
            OrganizerId = e.OrganizerId
        });
    }

    public async Task<Result<EventDto>> UpdateAsync(int id, UpdateEventDto dto, int userId, bool isAdmin)
    {
        var error = Validate(dto.Title, dto.Description, dto.Location, dto.StartsAt, dto.Price, dto.TotalSeats);
        if (error != null)
            return Result<EventDto>.Fail(error, ErrorType.Validation);

        var e = await _events.GetByIdAsync(id);
        if (e == null)
            return Result<EventDto>.Fail("Событие не найдено", ErrorType.NotFound);

        if (e.OrganizerId != userId && !isAdmin)
            return Result<EventDto>.Fail("Нельзя изменять чужое событие", ErrorType.Forbidden);

        var category = await _categories.GetByIdAsync(dto.CategoryId);
        if (category == null)
            return Result<EventDto>.Fail("Категория не найдена", ErrorType.NotFound);

        var newAvailable = e.AvailableSeats + (dto.TotalSeats - e.TotalSeats);
        if (newAvailable < 0)
            return Result<EventDto>.Fail("TotalSeats не может быть меньше числа занятых мест", ErrorType.Validation);

        e.Title = dto.Title!.Trim();
        e.Description = dto.Description?.Trim();
        e.Location = dto.Location!.Trim();
        e.StartsAt = dto.StartsAt;
        e.Price = dto.Price;
        e.TotalSeats = dto.TotalSeats;
        e.AvailableSeats = newAvailable;
        e.CategoryId = category.Id;
        e.Category = category;

        await _events.SaveChangesAsync();
        _cache.Invalidate(e.Id);

        return Result<EventDto>.Success(new EventDto
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            Location = e.Location,
            StartsAt = e.StartsAt,
            Price = e.Price,
            TotalSeats = e.TotalSeats,
            AvailableSeats = e.AvailableSeats,
            CreatedAt = e.CreatedAt,
            CategoryId = e.CategoryId,
            CategoryName = category.Name,
            OrganizerId = e.OrganizerId
        });
    }

    private static string? Validate(string? title, string? description, string? location, DateTime startsAt, decimal price, int totalSeats)
    {
        var t = title?.Trim();
        if (t == null || t.Length < 3 || t.Length > 200)
            return "Title должен содержать от 3 до 200 символов";

        var l = location?.Trim();
        if (l == null || l.Length < 2 || l.Length > 200)
            return "Location должен содержать от 2 до 200 символов";

        if (description != null && description.Length > 2000)
            return "Description не длиннее 2000 символов";

        if (price < 0)
            return "Price не может быть отрицательной";

        if (totalSeats < 1 || totalSeats > 100000)
            return "TotalSeats должно быть от 1 до 100000";

        if (startsAt <= DateTime.UtcNow)
            return "StartsAt должно быть в будущем";

        return null;
    }
}
