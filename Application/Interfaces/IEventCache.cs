using Application.ApiResult;
using Application.DTOs;
using Domain.Filters;

namespace Application.Interfaces;

public interface IEventCache
{
    string BuildListKey(EventFilter filter);
    EventDto? GetEvent(int id);
    void SetEvent(int id, EventDto dto);
    PagedResult<EventDto>? GetList(string key);
    void SetList(string key, PagedResult<EventDto> value);
    void Invalidate(int eventId);
}
