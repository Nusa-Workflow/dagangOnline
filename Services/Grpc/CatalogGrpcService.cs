using dagangOnline.Application.Services;
using dagangOnline.Data;
using dagangOnline.Domain;
using dagangOnline.Protos;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Services.Grpc;

public class CatalogGrpcService : CatalogGrpc.CatalogGrpcBase
{
    private readonly ApplicationDbContext _context;
    private readonly PublicCatalogService _catalogService;

    public CatalogGrpcService(ApplicationDbContext context, PublicCatalogService catalogService)
    {
        _context = context;
        _catalogService = catalogService;
    }

    public override async Task<GetServicesResponse> GetPublishedServices(GetServicesRequest request, ServerCallContext context)
    {
        var services = await _catalogService.GetPublishedServicesAsync();
        var response = new GetServicesResponse();

        foreach (var s in services)
        {
            response.Services.Add(new ServiceItem
            {
                Id = s.Id.ToString(),
                Name = s.Name,
                Slug = s.Slug,
                Summary = s.Summary,
                Description = s.Description,
                IsFeatured = s.IsFeatured
            });
        }

        return response;
    }

    public override async Task<GetProductsResponse> GetProducts(GetProductsRequest request, ServerCallContext context)
    {
        var query = _context.Products.AsNoTracking().Where(p => p.Status == PublicationStatus.Published);

        if (!string.IsNullOrWhiteSpace(request.Category) && Enum.TryParse<ProductCategory>(request.Category, true, out var cat))
        {
            query = query.Where(p => p.Category == cat);
        }

        var products = await query.OrderByDescending(p => p.CreatedAt).Take(50).ToListAsync();
        var response = new GetProductsResponse();

        foreach (var p in products)
        {
            response.Products.Add(new ProductItem
            {
                Id = p.Id.ToString(),
                Name = p.Name,
                Slug = p.Slug,
                Summary = p.Summary,
                Price = (double)p.Price,
                Currency = p.Currency ?? "IDR",
                Category = p.Category.ToString()
            });
        }

        return response;
    }

    public override async Task<GetPortfolioResponse> GetFeaturedPortfolio(GetPortfolioRequest request, ServerCallContext context)
    {
        var projects = await _catalogService.GetFeaturedPortfolioAsync();
        var response = new GetPortfolioResponse();

        foreach (var p in projects)
        {
            response.Items.Add(new PortfolioItem
            {
                Id = p.Id.ToString(),
                Title = p.Title,
                Summary = p.Summary,
                Problem = p.Problem ?? string.Empty,
                Solution = p.Solution ?? string.Empty,
                Result = p.Result ?? string.Empty,
                IsFeatured = p.IsFeatured
            });
        }

        return response;
    }

    public override async Task<SearchCatalogResponse> SearchCatalog(SearchCatalogRequest request, ServerCallContext context)
    {
        var results = await _catalogService.SearchAsync(request.Query);
        var response = new SearchCatalogResponse();

        foreach (var r in results)
        {
            response.Results.Add(new SearchResultItemMessage
            {
                Title = r.Title,
                Description = r.Description,
                Type = r.Type,
                Url = r.Url
            });
        }

        return response;
    }
}
