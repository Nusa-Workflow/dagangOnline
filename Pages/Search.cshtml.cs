using dagangOnline.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace dagangOnline.Pages;

public class SearchModel : PageModel
{
    private readonly PublicCatalogService _catalogService;

    public SearchModel(PublicCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public List<SearchResultItem> Results { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Q))
        {
            Results = await _catalogService.SearchAsync(Q);
        }
    }
}
