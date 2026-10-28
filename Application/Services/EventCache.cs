using System.Globalization;
using Application.ApiResult;
using Application.DTOs;
using Application.Interfaces;
using Domain.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class EventCache : IEventCache
{
    private static readonly TimeSpan Absolute = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Sliding = TimeSpan.FromMinutes(1);

    private readonly IMemoryCache _cache;
    private readonly ILogger<EventCache> _logger;
    private long _listVersion;

    public EventCache(IMemoryCache cache, ILogger<EventCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public string BuildListKey(EventFilter filter)
    {
        var version = Interlocked.Read(ref _listVersion);
        return string.Create(CultureInfo.InvariantCulture,
            $"events:list:v{version}:search={filter.Search?.Trim().ToLowerInvariant()}|category={filter.CategoryId}|min={filter.MinPrice}|max={filter.MaxPrice}|sort={filter.SortBy}|desc={filter.Desc}|page={filter.Page}|size={filter.PageSize}");
    }

    public EventDto? GetEvent(int id) => Get<EventDto>($"events:item:{id}");

    public void SetEvent(int id, EventDto dto) => Set($"events:item:{id}", dto);

    public PagedResult<EventDto>? GetList(string key) => Get<PagedResult<EventDto>>(key);

    public void SetList(string key, PagedResult<EventDto> value) => Set(key, value);

    public void Invalidate(int eventId)
    {
        _cache.Remove($"events:item:{eventId}");
        Interlocked.Increment(ref _listVersion);
    }

    private T? Get<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out T? value) && value != null)
        {
            _logger.LogInformation("Cache HIT key={Key}", key);
            return value;
        }

        _logger.LogInformation("Cache MISS key={Key}", key);
        return null;
    }

    private void Set<T>(string key, T value) where T : class
    {
        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Absolute,
            SlidingExpiration = Sliding
        });
    }
}
