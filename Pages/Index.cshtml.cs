using dagangOnline.Application.Services;
using dagangOnline.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dagangOnline.Pages;

public class IndexModel : PageModel
{
    private readonly PublicCatalogService _catalogService;

    public IndexModel(PublicCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public List<Service> FeaturedServices { get; set; } = new();
    public List<PortfolioProject> FeaturedProjects { get; set; } = new();

    public async Task OnGetAsync()
    {
        var services = await _catalogService.GetPublishedServicesAsync();
        FeaturedServices = services.Where(s => s.IsFeatured).Take(3).ToList();
        if (!FeaturedServices.Any())
        {
            FeaturedServices = services.Take(3).ToList();
        }

        var portfolio = await _catalogService.GetFeaturedPortfolioAsync();
        FeaturedProjects = portfolio.Take(2).ToList();
    }
}
