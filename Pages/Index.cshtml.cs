using dagangOnline.Application.DTOs;
using dagangOnline.Application.Services;
using dagangOnline.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dagangOnline.Pages;

public class IndexModel : PageModel
{
    private readonly dagangOnline.Application.Interfaces.ICatalogService _catalogService;

    public IndexModel(dagangOnline.Application.Interfaces.ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public List<ServiceDto> FeaturedServices { get; set; } = new();
    public List<PortfolioDto> FeaturedProjects { get; set; } = new();

    public async Task OnGetAsync()
    {
        var services = await _catalogService.Services.GetPublishedServicesAsync();
        FeaturedServices = services.Items.Where(s => s.IsFeatured).Take(3).ToList();
        if (!FeaturedServices.Any())
        {
            FeaturedServices = services.Items.Take(3).ToList();
        }

        var portfolio = await _catalogService.Portfolio.GetFeaturedPortfolioAsync();
        FeaturedProjects = portfolio.Items.Take(2).ToList();
    }
}
