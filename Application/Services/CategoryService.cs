using Application.ApiResult;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;

    public CategoryService(ICategoryRepository categories)
    {
        _categories = categories;
    }

    public async Task<Result<List<CategoryDto>>> GetAllAsync()
    {
        var categories = await _categories.GetAllAsync();

        return Result<List<CategoryDto>>.Success(
            categories.Select(c => new CategoryDto { Id = c.Id, Name = c.Name }).ToList());
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto)
    {
        var name = dto.Name?.Trim();
        if (name == null || name.Length < 2 || name.Length > 100)
            return Result<CategoryDto>.Fail("Name должно содержать от 2 до 100 символов", ErrorType.Validation);

        if (await _categories.ExistsByNameAsync(name))
            return Result<CategoryDto>.Fail("Категория с таким названием уже существует", ErrorType.Conflict);

        var category = new Category { Name = name };
        _categories.Add(category);
        await _categories.SaveChangesAsync();

        return Result<CategoryDto>.Success(new CategoryDto { Id = category.Id, Name = category.Name });
    }
}
