using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Data;
using dagangOnline.Domain;
using dagangOnline.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Infrastructure.Data.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Product> Items, int TotalCount)> GetPublishedProductsAsync(ProductCategory? category, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking().Where(p => p.Status == PublicationStatus.Published);
        
        if (category.HasValue)
        {
            query = query.Where(p => p.Category == category.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Product?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Products : _context.Products.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(string slug, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Products : _context.Products.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task<List<Product>> SearchPublishedAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var normalized = query.ToLower();
        return await _context.Products.AsNoTracking()
            .Where(x => x.Status == PublicationStatus.Published && (x.Name.ToLower().Contains(normalized) || x.Summary.ToLower().Contains(normalized)))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
