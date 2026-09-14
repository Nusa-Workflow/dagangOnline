using dagangOnline.Application.Services;
using dagangOnline.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dagangOnline.Pages;

public class PortfolioModel : PageModel
{
    private readonly PublicCatalogService _catalogService;

    public PortfolioModel(PublicCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public List<PortfolioProject> FeaturedProjects { get; set; } = new();

    public async Task OnGetAsync()
    {
        FeaturedProjects = await _catalogService.GetFeaturedPortfolioAsync();
    }
}
