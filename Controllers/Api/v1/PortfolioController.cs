using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dagangOnline.Controllers.Api.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PortfolioController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public PortfolioController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PortfolioDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken cancellationToken = default)
    {
        // Currently returns featured portfolios.
        var result = await _catalogService.Portfolio.GetFeaturedPortfolioAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<List<PortfolioDto>>.Ok(result.Items));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PortfolioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await _catalogService.Portfolio.GetPortfolioByIdAsync(id, cancellationToken);
        if (dto == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Portfolio tidak ditemukan."));
        }
        return Ok(ApiResponse<PortfolioDto>.Ok(dto));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<PortfolioDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePortfolioDto input, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currentUserName = User.Identity?.Name;
        var dto = await _catalogService.Portfolio.CreatePortfolioAsync(input, currentUserName, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<PortfolioDto>.Ok(dto, "Portfolio berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<PortfolioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePortfolioDto input, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currentUserName = User.Identity?.Name;
        try
        {
            var dto = await _catalogService.Portfolio.UpdatePortfolioAsync(id, input, currentUserName, cancellationToken);
            return Ok(ApiResponse<PortfolioDto>.Ok(dto, "Portfolio berhasil diperbarui."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Portfolio tidak ditemukan."));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserName = User.Identity?.Name;
        try
        {
            await _catalogService.Portfolio.DeletePortfolioAsync(id, currentUserName, cancellationToken);
            return Ok(ApiResponse<bool>.Ok(true, "Portfolio berhasil dihapus."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Portfolio tidak ditemukan."));
        }
    }
}
