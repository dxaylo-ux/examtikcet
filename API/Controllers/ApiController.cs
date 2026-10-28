using System.Security.Claims;
using Application.ApiResult;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Microsoft.AspNetCore.Mvc.ApiController]
[Route("api/[controller]")]
public abstract class ApiController : ControllerBase
{
    protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected bool IsAdmin => User.IsInRole("Admin");

    protected IActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Data);

        var body = new { error = result.ErrorMessage };

        return result.ErrorType switch
        {
            ErrorType.Validation => BadRequest(body),
            ErrorType.Unauthorized => Unauthorized(body),
            ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, body),
            ErrorType.NotFound => NotFound(body),
            ErrorType.Conflict => Conflict(body),
            _ => StatusCode(StatusCodes.Status500InternalServerError, body)
        };
    }
}
