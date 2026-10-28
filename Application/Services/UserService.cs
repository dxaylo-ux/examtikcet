using Application.ApiResult;
using Application.DTOs;
using Application.Identity;
using Application.Interfaces;
using Domain.Enums;
using Domain.Filters;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly UserManager<User> _userManager;

    public UserService(IUserRepository users, UserManager<User> userManager)
    {
        _users = users;
        _userManager = userManager;
    }

    public async Task<Result<PagedResult<UserDto>>> GetUsersAsync(UserFilter filter)
    {
        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize > 100) filter.PageSize = 100;
        if (filter.PageSize < 10) filter.PageSize = 10;

        var page = await _users.GetPagedAsync(filter);
        var roles = await _users.GetRolesAsync(page.Items.Select(u => u.Id));

        return Result<PagedResult<UserDto>>.Success(new PagedResult<UserDto>
        {
            Items = page.Items.Select(u => new UserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email ?? string.Empty,
                BirthDate = u.BirthDate,
                Role = roles.TryGetValue(u.Id, out var role) ? role : string.Empty
            }).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        });
    }

    public async Task<Result<UserDto>> ChangeRoleAsync(int id, ChangeRoleDto dto)
    {
        var requested = dto.Role?.Trim();
        string role;
        if (string.Equals(requested, nameof(UserRole.Organizer), StringComparison.OrdinalIgnoreCase))
            role = UserRole.Organizer.ToString();
        else if (string.Equals(requested, nameof(UserRole.User), StringComparison.OrdinalIgnoreCase))
            role = UserRole.User.ToString();
        else
            return Result<UserDto>.Fail("Допустимые роли: Organizer, User", ErrorType.Validation);

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return Result<UserDto>.Fail("Пользователь не найден", ErrorType.NotFound);

        var current = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, current);
        await _userManager.AddToRoleAsync(user, role);

        return Result<UserDto>.Success(new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            BirthDate = user.BirthDate,
            Role = role
        });
    }
}
