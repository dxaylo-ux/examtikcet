using Application.ApiResult;
using Application.Identity;
using Application.Interfaces;
using Domain.Filters;
using Infrastructure.Persistence.Data;
using Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<User>> GetPagedAsync(UserFilter filter)
    {
        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(u => u.FirstName.ToLower().Contains(search)
                                     || u.LastName.ToLower().Contains(search)
                                     || u.Email!.ToLower().Contains(search));
        }

        return await query.OrderBy(u => u.Id).ToPagedListAsync(filter.Page, filter.PageSize);
    }

    public async Task<Dictionary<int, string>> GetRolesAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.ToList();

        var pairs = await (from ur in _context.UserRoles
                           join r in _context.Roles on ur.RoleId equals r.Id
                           where ids.Contains(ur.UserId)
                           select new { ur.UserId, r.Name }).ToListAsync();

        return pairs.GroupBy(p => p.UserId).ToDictionary(g => g.Key, g => g.First().Name ?? string.Empty);
    }
}
