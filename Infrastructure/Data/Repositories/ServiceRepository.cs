using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Domain;
using dagangOnline.Domain.Repositories;
using dagangOnline.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Infrastructure.Data.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly ApplicationDbContext _context;

    public ServiceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Service> Items, int TotalCount)> GetPublishedServicesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Services.AsNoTracking().Where(s => s.Status == PublicationStatus.Published);
        
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<Service>> GetPublishedServicesAsync(bool? isFeatured, CancellationToken cancellationToken = default)
    {
        var query = _context.Services.AsNoTracking().Where(s => s.Status == PublicationStatus.Published);
        
        if (isFeatured.HasValue)
        {
            query = query.Where(s => s.IsFeatured == isFeatured.Value);
        }

        return await query.OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<Service?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Services : _context.Services.AsNoTracking();
        return await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Service?> GetBySlugAsync(string slug, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.Services : _context.Services.AsNoTracking();
        return await query.FirstOrDefaultAsync(s => s.Slug == slug, cancellationToken);
    }

    public async Task<List<Service>> SearchPublishedAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var normalized = query.ToLower();
        return await _context.Services.AsNoTracking()
            .Where(x => x.Status == PublicationStatus.Published && (x.Name.ToLower().Contains(normalized) || x.Summary.ToLower().Contains(normalized)))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Service service, CancellationToken cancellationToken = default)
    {
        await _context.Services.AddAsync(service, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Service service, CancellationToken cancellationToken = default)
    {
        _context.Services.Update(service);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Service service, CancellationToken cancellationToken = default)
    {
        _context.Services.Remove(service);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
