using dagangOnline.Application.DTOs;
using dagangOnline.Application.Services;
using dagangOnline.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dagangOnline.Pages;

public class ServicesModel : PageModel
{
    private readonly dagangOnline.Application.Interfaces.ICatalogService _catalogService;

    public ServicesModel(dagangOnline.Application.Interfaces.ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public List<ServiceDto> PublishedServices { get; set; } = new();

    public async Task OnGetAsync()
    {
        PublishedServices = (await _catalogService.Services.GetPublishedServicesAsync()).Items;
    }
}
