using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace dagangOnline.Domain.Repositories;

public interface IProductRepository
{
    Task<(List<Product> Items, int TotalCount)> GetPublishedProductsAsync(ProductCategory? category, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<Product?> GetBySlugAsync(string slug, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<List<Product>> SearchPublishedAsync(string query, int limit, CancellationToken cancellationToken = default);
    
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);
}
