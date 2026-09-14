using dagangOnline.Application.Services;
using dagangOnline.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dagangOnline.Pages;

public class ServicesModel : PageModel
{
    private readonly PublicCatalogService _catalogService;

    public ServicesModel(PublicCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public List<Service> PublishedServices { get; set; } = new();

    public async Task OnGetAsync()
    {
        PublishedServices = await _catalogService.GetPublishedServicesAsync();
    }
}
