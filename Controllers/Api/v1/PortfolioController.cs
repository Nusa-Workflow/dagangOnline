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
public class PortfolioController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;

    public PortfolioController(ApplicationDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PortfolioDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool? isFeatured = null)
    {
        var query = _context.PortfolioProjects.AsNoTracking().Where(p => p.Status == PublicationStatus.Published);

        if (isFeatured.HasValue)
        {
            query = query.Where(p => p.IsFeatured == isFeatured.Value);
        }

        var list = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PortfolioDto
            {
                Id = p.Id,
                Title = p.Title,
                Summary = p.Summary,
                Description = p.Description,
                Problem = p.Problem,
                Solution = p.Solution,
                Result = p.Result,
                Status = p.Status,
                IsFeatured = p.IsFeatured,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<PortfolioDto>>.Ok(list));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PortfolioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var project = await _context.PortfolioProjects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (project == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Portfolio tidak ditemukan."));
        }

        var dto = new PortfolioDto
        {
            Id = project.Id,
            Title = project.Title,
            Summary = project.Summary,
            Description = project.Description,
            Problem = project.Problem,
            Solution = project.Solution,
            Result = project.Result,
            Status = project.Status,
            IsFeatured = project.IsFeatured,
            CreatedAt = project.CreatedAt
        };

        return Ok(ApiResponse<PortfolioDto>.Ok(dto));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<PortfolioDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePortfolioDto input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var entity = new PortfolioProject
        {
            Title = input.Title,
            Summary = input.Summary,
            Description = input.Description ?? string.Empty,
            Problem = input.Problem ?? string.Empty,
            Solution = input.Solution ?? string.Empty,
            Result = input.Result ?? string.Empty,
            Status = input.Status,
            IsFeatured = input.IsFeatured,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PortfolioProjects.Add(entity);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "CreatePortfolioApi",
            entityName: "PortfolioProject",
            entityId: entity.Id.ToString(),
            performedByUserName: User?.Identity?.Name,
            details: $"Portfolio '{entity.Title}' created via REST API"
        );

        var dto = new PortfolioDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Summary = entity.Summary,
            Description = entity.Description,
            Problem = entity.Problem,
            Solution = entity.Solution,
            Result = entity.Result,
            Status = entity.Status,
            IsFeatured = entity.IsFeatured,
            CreatedAt = entity.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ApiResponse<PortfolioDto>.Ok(dto, "Portfolio berhasil dibuat."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<PortfolioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePortfolioDto input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var entity = await _context.PortfolioProjects.FindAsync(id);
        if (entity == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Portfolio tidak ditemukan."));
        }

        entity.Title = input.Title;
        entity.Summary = input.Summary;
        entity.Description = input.Description ?? string.Empty;
        entity.Problem = input.Problem ?? string.Empty;
        entity.Solution = input.Solution ?? string.Empty;
        entity.Result = input.Result ?? string.Empty;
        entity.Status = input.Status;
        entity.IsFeatured = input.IsFeatured;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "UpdatePortfolioApi",
            entityName: "PortfolioProject",
            entityId: id.ToString(),
            performedByUserName: User?.Identity?.Name,
            details: $"Portfolio '{entity.Title}' updated via REST API"
        );

        var dto = new PortfolioDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Summary = entity.Summary,
            Description = entity.Description,
            Problem = entity.Problem,
            Solution = entity.Solution,
            Result = entity.Result,
            Status = entity.Status,
            IsFeatured = entity.IsFeatured,
            CreatedAt = entity.CreatedAt
        };

        return Ok(ApiResponse<PortfolioDto>.Ok(dto, "Portfolio berhasil diperbarui."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.PortfolioProjects.FindAsync(id);
        if (entity == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Portfolio tidak ditemukan."));
        }

        _context.PortfolioProjects.Remove(entity);
        await _context.SaveChangesAsync();

        await _auditLogService.LogWarningAsync(
            action: "DeletePortfolioApi",
            entityName: "PortfolioProject",
            entityId: id.ToString(),
            performedByUserName: User?.Identity?.Name,
            details: $"Portfolio '{entity.Title}' deleted via REST API"
        );

        return Ok(ApiResponse<bool>.Ok(true, "Portfolio berhasil dihapus."));
    }
}
