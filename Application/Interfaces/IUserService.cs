using Application.ApiResult;
using Application.DTOs;
using Domain.Filters;

namespace Application.Interfaces;

public interface IUserService
{
    Task<Result<PagedResult<UserDto>>> GetUsersAsync(UserFilter filter);
    Task<Result<UserDto>> ChangeRoleAsync(int id, ChangeRoleDto dto);
}
