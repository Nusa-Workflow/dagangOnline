using dagangOnline.Data;
using dagangOnline.Domain;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Application.Services;

public class PublicCatalogService
{
    private readonly ApplicationDbContext _context;

    public PublicCatalogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PortfolioProject>> GetFeaturedPortfolioAsync()
    {
        return await _context.PortfolioProjects
            .AsNoTracking()
            .Where(x => x.Status == PublicationStatus.Published && x.IsFeatured)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Service>> GetPublishedServicesAsync()
    {
        return await _context.Services
            .AsNoTracking()
            .Where(x => x.Status == PublicationStatus.Published)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
    public async Task<List<SearchResultItem>> SearchAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<SearchResultItem>();
        }

        var normalized = query.Trim().ToLower();
        var results = new List<SearchResultItem>();

        var services = await _context.Services
            .AsNoTracking()
            .Where(x => x.Status == PublicationStatus.Published &&
                        (x.Name.ToLower().Contains(normalized) || x.Summary.ToLower().Contains(normalized)))
            .Take(10)
            .ToListAsync();

        results.AddRange(services.Select(s => new SearchResultItem
        {
            Title = s.Name,
            Description = s.Summary,
            Type = "Service",
            Url = $"/Services"
        }));

        var projects = await _context.PortfolioProjects
            .AsNoTracking()
            .Where(x => x.Status == PublicationStatus.Published &&
                        (x.Title.ToLower().Contains(normalized) || x.Summary.ToLower().Contains(normalized)))
            .Take(10)
            .ToListAsync();

        results.AddRange(projects.Select(p => new SearchResultItem
        {
            Title = p.Title,
            Description = p.Summary,
            Type = "Portfolio",
            Url = "/Portfolio"
        }));

        var products = await _context.Products
            .AsNoTracking()
            .Where(x => x.Status == PublicationStatus.Published &&
                        (x.Name.ToLower().Contains(normalized) || x.Summary.ToLower().Contains(normalized)))
            .Take(10)
            .ToListAsync();

        results.AddRange(products.Select(pr => new SearchResultItem
        {
            Title = pr.Name,
            Description = pr.Summary,
            Type = "Product",
            Url = $"/Services"
        }));

        return results;
    }
}

public class SearchResultItem
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
