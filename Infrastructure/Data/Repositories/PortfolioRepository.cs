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

public class PortfolioRepository : IPortfolioRepository
{
    private readonly ApplicationDbContext _context;

    public PortfolioRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(List<PortfolioProject> Items, int TotalCount)> GetFeaturedPortfolioAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.PortfolioProjects.AsNoTracking()
            .Where(p => p.Status == PublicationStatus.Published && p.IsFeatured);
        
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<List<PortfolioProject>> GetPublishedPortfolioAsync(bool? isFeatured, CancellationToken cancellationToken = default)
    {
        var query = _context.PortfolioProjects.AsNoTracking().Where(p => p.Status == PublicationStatus.Published);
        
        if (isFeatured.HasValue)
        {
            query = query.Where(p => p.IsFeatured == isFeatured.Value);
        }

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<PortfolioProject?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _context.PortfolioProjects : _context.PortfolioProjects.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<PortfolioProject>> SearchPublishedAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var normalized = query.ToLower();
        return await _context.PortfolioProjects.AsNoTracking()
            .Where(x => x.Status == PublicationStatus.Published && (x.Title.ToLower().Contains(normalized) || x.Summary.ToLower().Contains(normalized)))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PortfolioProject project, CancellationToken cancellationToken = default)
    {
        await _context.PortfolioProjects.AddAsync(project, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PortfolioProject project, CancellationToken cancellationToken = default)
    {
        _context.PortfolioProjects.Update(project);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PortfolioProject project, CancellationToken cancellationToken = default)
    {
        _context.PortfolioProjects.Remove(project);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
