using dagangOnline.Application.DTOs;
using dagangOnline.Application.Services;
using dagangOnline.Authorization;
using dagangOnline.Data;
using dagangOnline.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Controllers.Api.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ContactController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ContactInquiryService _inquiryService;
    private readonly AuditLogService _auditLogService;

    public ContactController(ApplicationDbContext context, ContactInquiryService inquiryService, AuditLogService auditLogService)
    {
        _context = context;
        _inquiryService = inquiryService;
        _auditLogService = auditLogService;
    }

    [HttpPost("inquiries")]
    [EnableRateLimiting("contact")]
    [ProducesResponseType(typeof(ApiResponse<ContactInquiryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitInquiry([FromBody] ContactInquirySubmission submission)
    {
        if (string.IsNullOrWhiteSpace(submission.Name) || string.IsNullOrWhiteSpace(submission.Email) || string.IsNullOrWhiteSpace(submission.Subject) || string.IsNullOrWhiteSpace(submission.Message))
        {
            return BadRequest(Problem(statusCode: StatusCodes.Status400BadRequest, title: "Validasi gagal.", detail: "Nama, email, subjek, dan pesan wajib diisi."));
        }

        if (!submission.ConsentAccepted)
        {
            return BadRequest(Problem(statusCode: StatusCodes.Status400BadRequest, title: "Persetujuan privasi wajib diterima."));
        }

        var entity = await _inquiryService.SubmitAsync(submission);

        var dto = new ContactInquiryDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Email = entity.Email,
            Subject = entity.Subject,
            Message = entity.Message,
            Category = entity.Category,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        };

        return CreatedAtAction(nameof(GetInquiryById), new { id = entity.Id }, ApiResponse<ContactInquiryDto>.Ok(dto, "Inquiri berhasil dikirim."));
    }

    [HttpGet("inquiries")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<List<ContactInquiryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllInquiries([FromQuery] InquiryStatus? status = null)
    {
        var query = _context.ContactInquiries.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        var list = await query
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new ContactInquiryDto
            {
                Id = i.Id,
                Name = i.Name,
                Email = i.Email,
                Subject = i.Subject,
                Message = i.Message,
                Category = i.Category,
                Status = i.Status,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ContactInquiryDto>>.Ok(list));
    }

    [HttpGet("inquiries/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ContactInquiryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInquiryById(Guid id)
    {
        var inq = await _context.ContactInquiries.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        if (inq == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Inquiri tidak ditemukan."));
        }

        var dto = new ContactInquiryDto
        {
            Id = inq.Id,
            Name = inq.Name,
            Email = inq.Email,
            Subject = inq.Subject,
            Message = inq.Message,
            Category = inq.Category,
            Status = inq.Status,
            CreatedAt = inq.CreatedAt
        };

        return Ok(ApiResponse<ContactInquiryDto>.Ok(dto));
    }

    [HttpPatch("inquiries/{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ContactInquiryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateInquiryStatusDto input)
    {
        var inq = await _context.ContactInquiries.FindAsync(id);
        if (inq == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Inquiri tidak ditemukan."));
        }

        var old = inq.Status;
        inq.Status = input.Status;
        inq.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            action: "UpdateInquiryStatusApi",
            entityName: "ContactInquiry",
            entityId: inq.Id.ToString(),
            performedByUserName: User?.Identity?.Name,
            details: $"Changed status from {old} to {input.Status}"
        );

        var dto = new ContactInquiryDto
        {
            Id = inq.Id,
            Name = inq.Name,
            Email = inq.Email,
            Subject = inq.Subject,
            Message = inq.Message,
            Category = inq.Category,
            Status = inq.Status,
            CreatedAt = inq.CreatedAt
        };

        return Ok(ApiResponse<ContactInquiryDto>.Ok(dto, "Status inquiri berhasil diperbarui."));
    }

    [HttpDelete("inquiries/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInquiry(Guid id)
    {
        var inq = await _context.ContactInquiries.FindAsync(id);
        if (inq == null)
        {
            return NotFound(Problem(statusCode: StatusCodes.Status404NotFound, title: "Inquiri tidak ditemukan."));
        }

        _context.ContactInquiries.Remove(inq);
        await _context.SaveChangesAsync();

        await _auditLogService.LogWarningAsync(
            action: "DeleteInquiryApi",
            entityName: "ContactInquiry",
            entityId: id.ToString(),
            performedByUserName: User?.Identity?.Name,
            details: $"Deleted inquiry '{inq.Subject}'"
        );

        return Ok(ApiResponse<bool>.Ok(true, "Inquiri berhasil dihapus."));
    }
}
