using System.Security.Claims;
using dagangOnline.Application.DTOs;
using dagangOnline.Authorization;
using dagangOnline.Models.Agents;
using dagangOnline.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dagangOnline.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.RequireAgentOrAdmin)]
[Produces("application/json")]
public class AgentController : ControllerBase
{
    private readonly AgentReviewService _agentReviewService;

    public AgentController(AgentReviewService agentReviewService)
    {
        _agentReviewService = agentReviewService;
    }

    [HttpGet("queue")]
    [ProducesResponseType(typeof(ApiResponse<List<ReviewQueueItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueue()
    {
        var items = await _agentReviewService.GetPendingTasksAsync();
        return Ok(ApiResponse<List<ReviewQueueItemDto>>.Ok(items));
    }

    [HttpPost("approve")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve([FromBody] ReviewDecisionInput input)
    {
        if (input.TaskId == Guid.Empty)
        {
            return BadRequest(new { message = "TaskId tidak valid." });
        }

        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "agent-system";
        var reviewerName = User.Identity?.Name ?? "Human Agent";

        var success = await _agentReviewService.ApproveProductAsync(input.TaskId, reviewerId, reviewerName, input.Notes);
        if (!success)
        {
            return NotFound(new { message = "Tugas review atau produk tidak ditemukan." });
        }

        return Ok(ApiResponse<bool>.Ok(true, "Produk berhasil disetujui dan diterbitkan."));
    }

    [HttpPost("reject")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject([FromBody] ReviewDecisionInput input)
    {
        if (input.TaskId == Guid.Empty)
        {
            return BadRequest(new { message = "TaskId tidak valid." });
        }

        if (string.IsNullOrWhiteSpace(input.Reason))
        {
            return BadRequest(new { message = "Alasan penolakan wajib disertakan." });
        }

        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "agent-system";
        var reviewerName = User.Identity?.Name ?? "Human Agent";

        var success = await _agentReviewService.RejectProductAsync(input.TaskId, reviewerId, reviewerName, input.Reason, input.Notes);
        if (!success)
        {
            return NotFound(new { message = "Tugas review atau produk tidak ditemukan." });
        }

        return Ok(ApiResponse<bool>.Ok(true, "Produk telah ditolak dengan catatan alasan untuk Mitra."));
    }
}
