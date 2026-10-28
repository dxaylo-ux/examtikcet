using Application.ApiResult;
using Application.Identity;
using Domain.Filters;

namespace Application.Interfaces;

public interface IUserRepository
{
    Task<PagedResult<User>> GetPagedAsync(UserFilter filter);
    Task<Dictionary<int, string>> GetRolesAsync(IEnumerable<int> userIds);
}
