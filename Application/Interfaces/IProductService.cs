using System;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;
using dagangOnline.Domain;

namespace dagangOnline.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPublishedProductsAsync(ProductCategory? category, int page = 1, int pageSize = 100, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto, string? userId, string? userName, bool isMitra, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto dto, string? userId, string? userName, bool isMitra, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, string? userId, string? userName, bool isMitra, CancellationToken cancellationToken = default);
}
