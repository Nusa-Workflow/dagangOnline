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
public class ServicesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;

    public ServicesController(ApplicationDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ServiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PublicationStatus? status = null, [FromQuery] bool? isFeatured = null)
    {
        var query = _context.Services.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }
        else
        {
            query = query.Where(s => s.Status == PublicationStatus.Published);
        }

        if (isFeatured.HasValue)
        {
            query = query.Where(s => s.IsFeatured == isFeatured.Value);
        }

        var services = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Slug = s.Slug,
                Summary = s.Summary,
                Description = s.Description,
                Status = s.Status,
                IsFeatured = s.IsFeatured,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ServiceDto>>.Ok(services));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (service == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Layanan tidak ditemukan.", detail: $"Layanan dengan ID {id} tidak terdaftar."));
        }

        var dto = new ServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Slug = service.Slug,
            Summary = service.Summary,
            Description = service.Description,
            Status = service.Status,
            IsFeatured = service.IsFeatured,
            CreatedAt = service.CreatedAt
        };

        return Ok(ApiResponse<ServiceDto>.Ok(dto));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateServiceDto input)
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

        var entity = new Service
        {
            Name = input.Name,
            Slug = uniqueSlug,
            Summary = input.Summary,
            Description = input.Description ?? string.Empty,
            Status = input.Status,
            IsFeatured = input.IsFeatured,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Services.Add(entity);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "CreateServiceApi",
            entityName: "Service",
            entityId: entity.Id.ToString(),
            performedByUserName: User?.Identity?.Name,
            details: $"Service '{entity.Name}' created via REST API"
        );

        var dto = new ServiceDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            Summary = entity.Summary,
            Description = entity.Description,
            Status = entity.Status,
            IsFeatured = entity.IsFeatured,
            CreatedAt = entity.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ApiResponse<ServiceDto>.Ok(dto, "Layanan berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceDto input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var entity = await _context.Services.FindAsync(id);
        if (entity == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Layanan tidak ditemukan."));
        }

        entity.Name = input.Name;
        if (!string.IsNullOrWhiteSpace(input.Slug)) entity.Slug = input.Slug;
        entity.Summary = input.Summary;
        entity.Description = input.Description ?? string.Empty;
        entity.Status = input.Status;
        entity.IsFeatured = input.IsFeatured;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "UpdateServiceApi",
            entityName: "Service",
            entityId: id.ToString(),
            performedByUserName: User?.Identity?.Name,
            details: $"Service '{entity.Name}' updated via REST API"
        );

        var dto = new ServiceDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            Summary = entity.Summary,
            Description = entity.Description,
            Status = entity.Status,
            IsFeatured = entity.IsFeatured,
            CreatedAt = entity.CreatedAt
        };

        return Ok(ApiResponse<ServiceDto>.Ok(dto, "Layanan berhasil diperbarui."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.Services.FindAsync(id);
        if (entity == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Layanan tidak ditemukan."));
        }

        _context.Services.Remove(entity);
        await _context.SaveChangesAsync();

        await _auditLogService.LogWarningAsync(
            action: "DeleteServiceApi",
            entityName: "Service",
            entityId: id.ToString(),
            performedByUserName: User?.Identity?.Name,
            details: $"Service '{entity.Name}' deleted via REST API"
        );

        return Ok(ApiResponse<bool>.Ok(true, "Layanan berhasil dihapus."));
    }
}
