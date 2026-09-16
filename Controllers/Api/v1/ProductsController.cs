using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Authorization;
using dagangOnline.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dagangOnline.Controllers.Api.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public ProductsController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ProductCategory? category = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken cancellationToken = default)
    {
        var result = await _catalogService.Products.GetPublishedProductsAsync(category, page, pageSize, cancellationToken);
        return Ok(ApiResponse<List<ProductDto>>.Ok(result.Items));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var dto = await _catalogService.Products.GetProductByIdAsync(id, cancellationToken);
        if (dto == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Produk tidak ditemukan."));
        }
        return Ok(ApiResponse<ProductDto>.Ok(dto));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireMitraOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto input, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var isMitra = User.IsInRole(RoleConstants.Mitra) && !User.IsInRole(RoleConstants.Admin);
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserName = User.Identity?.Name;

        var dto = await _catalogService.Products.CreateProductAsync(input, currentUserId, currentUserName, isMitra, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<ProductDto>.Ok(dto, "Produk berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireMitraOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto input, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var isMitra = User.IsInRole(RoleConstants.Mitra) && !User.IsInRole(RoleConstants.Admin);
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserName = User.Identity?.Name;

        try
        {
            var dto = await _catalogService.Products.UpdateProductAsync(id, input, currentUserId, currentUserName, isMitra, cancellationToken);
            return Ok(ApiResponse<ProductDto>.Ok(dto, "Produk berhasil diperbarui."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Produk tidak ditemukan."));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireMitraOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var isMitra = User.IsInRole(RoleConstants.Mitra) && !User.IsInRole(RoleConstants.Admin);
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserName = User.Identity?.Name;

        try
        {
            await _catalogService.Products.DeleteProductAsync(id, currentUserId, currentUserName, isMitra, cancellationToken);
            return Ok(ApiResponse<bool>.Ok(true, "Produk berhasil dihapus."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Produk tidak ditemukan."));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
