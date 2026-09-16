using dagangOnline.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Pages;

public class SearchModel : PageModel
{
    private readonly dagangOnline.Application.Interfaces.ICatalogService _catalogService;

    public SearchModel(dagangOnline.Application.Interfaces.ICatalogService catalogService)
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
            Results = (await _catalogService.Search.SearchAsync(Q)).Items;
        }
    }
}
