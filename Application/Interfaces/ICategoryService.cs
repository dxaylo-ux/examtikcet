using Application.ApiResult;
using Application.DTOs;

namespace Application.Interfaces;

public interface ICategoryService
{
    Task<Result<List<CategoryDto>>> GetAllAsync();
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto);
}
