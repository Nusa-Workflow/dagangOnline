using dagangOnline.Application.DTOs;
using dagangOnline.Application.Services;
using dagangOnline.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dagangOnline.Pages;

public class PortfolioModel : PageModel
{
    private readonly dagangOnline.Application.Interfaces.ICatalogService _catalogService;

    public PortfolioModel(dagangOnline.Application.Interfaces.ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public List<PortfolioDto> FeaturedProjects { get; set; } = new();

    public async Task OnGetAsync()
    {
        FeaturedProjects = (await _catalogService.Portfolio.GetFeaturedPortfolioAsync()).Items;
    }
}
