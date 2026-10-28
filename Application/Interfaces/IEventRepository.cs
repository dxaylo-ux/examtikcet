using Application.ApiResult;
using Domain.Entities;
using Domain.Filters;

namespace Application.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int id);
    Task<PagedResult<Event>> GetPagedAsync(EventFilter filter);
    void Add(Event ev);
    Task SaveChangesAsync();
}
