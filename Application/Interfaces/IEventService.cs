using Application.ApiResult;
using Application.DTOs;
using Domain.Filters;

namespace Application.Interfaces;

public interface IEventService
{
    Task<Result<PagedResult<EventDto>>> GetListAsync(EventFilter filter);
    Task<Result<EventDto>> GetByIdAsync(int id);
    Task<Result<EventDto>> CreateAsync(CreateEventDto dto, int organizerId);
    Task<Result<EventDto>> UpdateAsync(int id, UpdateEventDto dto, int userId, bool isAdmin);
}
