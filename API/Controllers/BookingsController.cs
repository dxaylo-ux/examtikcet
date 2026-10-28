using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class BookingsController : ApiController
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingDto dto)
    {
        return ToResponse(await _bookingService.CreateAsync(dto, CurrentUserId));
    }

    [HttpGet("my")]
    public async Task<IActionResult> My([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return ToResponse(await _bookingService.GetMyAsync(CurrentUserId, status, page, pageSize));
    }

    [HttpPost("{id:int}/pay")]
    public async Task<IActionResult> Pay(int id)
    {
        return ToResponse(await _bookingService.PayAsync(id, CurrentUserId));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        return ToResponse(await _bookingService.CancelAsync(id, CurrentUserId, IsAdmin));
    }
}
