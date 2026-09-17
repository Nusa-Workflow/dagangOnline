using dagangOnline.Application.DTOs;
using dagangOnline.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace dagangOnline.Controllers.Api.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly dagangOnline.Application.Interfaces.ICatalogService _catalogService;

    public SearchController(dagangOnline.Application.Interfaces.ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SearchResultItem>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? q)
    {
        var results = await _catalogService.Search.SearchAsync(q ?? string.Empty);
        return Ok(ApiResponse<List<SearchResultItem>>.Ok(results.Items));
    }
}
