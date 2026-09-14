using System.Text.RegularExpressions;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Services;
using dagangOnline.Authorization;
using dagangOnline.Data;
using dagangOnline.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Controllers.Api.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;

    public ProductsController(ApplicationDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ProductCategory? category = null, [FromQuery] decimal? maxPrice = null)
    {
        var query = _context.Products.AsNoTracking().Where(p => p.Status == PublicationStatus.Published);

        if (category.HasValue)
        {
            query = query.Where(p => p.Category == category.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Summary = p.Summary,
                Description = p.Description,
                Price = p.Price,
                Currency = p.Currency ?? "IDR",
                Category = p.Category,
                Status = p.Status,
                IsFeatured = p.IsFeatured,
                ServiceId = p.ServiceId,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ProductDto>>.Ok(products));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Produk tidak ditemukan."));
        }

        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Summary = product.Summary,
            Description = product.Description,
            Price = product.Price,
            Currency = product.Currency ?? "IDR",
            Category = product.Category,
            Status = product.Status,
            IsFeatured = product.IsFeatured,
            ServiceId = product.ServiceId,
            CreatedAt = product.CreatedAt
        };

        return Ok(ApiResponse<ProductDto>.Ok(dto));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireMitraOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var slug = string.IsNullOrWhiteSpace(input.Slug)
            ? Regex.Replace(input.Name.ToLowerInvariant(), @"[^a-z0-9\s-]", "")
            : input.Slug;
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        var uniqueSlug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 6)}";

        var entity = new Product
        {
            Name = input.Name,
            Slug = uniqueSlug,
            Summary = input.Summary,
            Description = input.Description ?? string.Empty,
            Price = input.Price,
            Currency = input.Currency,
            Category = input.Category,
            Status = input.Status,
            IsFeatured = input.IsFeatured,
            ServiceId = input.ServiceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(entity);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "CreateProductApi",
            entityName: "Product",
            entityId: entity.Id.ToString(),
            performedByUserName: User?.Identity?.Name,
            details: $"Product '{entity.Name}' created via REST API"
        );

        var dto = new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            Summary = entity.Summary,
            Description = entity.Description,
            Price = entity.Price,
            Currency = entity.Currency ?? "IDR",
            Category = entity.Category,
            Status = entity.Status,
            IsFeatured = entity.IsFeatured,
            ServiceId = entity.ServiceId,
            CreatedAt = entity.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ApiResponse<ProductDto>.Ok(dto, "Produk berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireMitraOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var entity = await _context.Products.FindAsync(id);
        if (entity == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Produk tidak ditemukan."));
        }

        entity.Name = input.Name;
        if (!string.IsNullOrWhiteSpace(input.Slug)) entity.Slug = input.Slug;
        entity.Summary = input.Summary;
        entity.Description = input.Description ?? string.Empty;
        entity.Price = input.Price;
        entity.Currency = input.Currency;
        entity.Category = input.Category;
        entity.Status = input.Status;
        entity.IsFeatured = input.IsFeatured;
        entity.ServiceId = input.ServiceId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "UpdateProductApi",
            entityName: "Product",
            entityId: id.ToString(),
            performedByUserName: User?.Identity?.Name,
            details: $"Product '{entity.Name}' updated via REST API"
        );

        var dto = new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            Summary = entity.Summary,
            Description = entity.Description,
            Price = entity.Price,
            Currency = entity.Currency ?? "IDR",
            Category = entity.Category,
            Status = entity.Status,
            IsFeatured = entity.IsFeatured,
            ServiceId = entity.ServiceId,
            CreatedAt = entity.CreatedAt
        };

        return Ok(ApiResponse<ProductDto>.Ok(dto, "Produk berhasil diperbarui."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireMitraOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.Products.FindAsync(id);
        if (entity == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Produk tidak ditemukan."));
        }

        _context.Products.Remove(entity);
        await _context.SaveChangesAsync();

        await _auditLogService.LogWarningAsync(
            action: "DeleteProductApi",
            entityName: "Product",
            entityId: id.ToString(),
            performedByUserName: User?.Identity?.Name,
            details: $"Product '{entity.Name}' deleted via REST API"
        );

        return Ok(ApiResponse<bool>.Ok(true, "Produk berhasil dihapus."));
    }
}
