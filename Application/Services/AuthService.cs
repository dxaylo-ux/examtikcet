using Application.ApiResult;
using Application.DTOs;
using Application.Identity;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenGenerator _jwt;

    public AuthService(UserManager<User> userManager, IJwtTokenGenerator jwt)
    {
        _userManager = userManager;
        _jwt = jwt;
    }

    public async Task<Result<UserDto>> RegisterAsync(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName) ||
            string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return Result<UserDto>.Fail("FirstName, LastName, Email и Password обязательны", ErrorType.Validation);

        if (dto.FirstName.Trim().Length > 250 || dto.LastName.Trim().Length > 250)
            return Result<UserDto>.Fail("FirstName и LastName не длиннее 250 символов", ErrorType.Validation);

        var email = dto.Email.Trim().ToLowerInvariant();
        if (!email.Contains('@'))
            return Result<UserDto>.Fail("Некорректный email", ErrorType.Validation);

        if (await _userManager.FindByEmailAsync(email) != null)
            return Result<UserDto>.Fail("Email уже занят", ErrorType.Conflict);

        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            BirthDate = dto.BirthDate
        };

        var created = await _userManager.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
            return Result<UserDto>.Fail(string.Join("; ", created.Errors.Select(e => e.Description)), ErrorType.Validation);

        await _userManager.AddToRoleAsync(user, UserRole.User.ToString());

        return Result<UserDto>.Success(new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            BirthDate = user.BirthDate,
            Role = UserRole.User.ToString()
        });
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return Result<AuthResponseDto>.Fail("Email и Password обязательны", ErrorType.Validation);

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLowerInvariant());
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Result<AuthResponseDto>.Fail("Неверный email или пароль", ErrorType.Unauthorized);

        var roles = await _userManager.GetRolesAsync(user);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            Token = _jwt.Generate(user, roles),
            User = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                BirthDate = user.BirthDate,
                Role = roles.FirstOrDefault() ?? string.Empty
            }
        });
    }

    public async Task<Result<UserDto>> GetMeAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Result<UserDto>.Fail("Пользователь не найден", ErrorType.Unauthorized);

        var roles = await _userManager.GetRolesAsync(user);

        return Result<UserDto>.Success(new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            BirthDate = user.BirthDate,
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }
}
