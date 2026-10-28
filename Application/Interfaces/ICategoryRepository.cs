using Domain.Entities;

namespace Application.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<bool> ExistsByNameAsync(string name);
    void Add(Category category);
    Task SaveChangesAsync();
}
