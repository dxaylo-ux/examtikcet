using Application.DTOs;
using Application.Interfaces;
using Domain.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class EventsController : ApiController
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EventFilter filter)
    {
        return ToResponse(await _eventService.GetListAsync(filter));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        return ToResponse(await _eventService.GetByIdAsync(id));
    }

    [Authorize(Roles = "Organizer,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateEventDto dto)
    {
        return ToResponse(await _eventService.CreateAsync(dto, CurrentUserId));
    }

    [Authorize(Roles = "Organizer,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEventDto dto)
    {
        return ToResponse(await _eventService.UpdateAsync(id, dto, CurrentUserId, IsAdmin));
    }
}
