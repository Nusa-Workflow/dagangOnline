using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;

namespace dagangOnline.Controllers.Api.v1;

[ApiController]
[Route("api/v1/feedback")]
[Produces("application/json")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackSubmissionDto dto, CancellationToken cancellationToken)
    {
        if (dto.ConversationId == Guid.Empty || dto.MessageId == Guid.Empty)
        {
            return BadRequest(ApiResponse<bool>.Fail("ConversationId dan MessageId wajib disertakan."));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var success = await _feedbackService.SubmitFeedbackAsync(dto, userId, cancellationToken);

        return Ok(ApiResponse<bool>.Ok(success, "Umpan balik berhasil dikirim. Terima kasih!"));
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary([FromQuery] int? days, CancellationToken cancellationToken)
    {
        DateTime? since = days.HasValue ? DateTime.UtcNow.AddDays(-days.Value) : null;
        var summary = await _feedbackService.GetFeedbackSummaryAsync(since, cancellationToken);
        return Ok(ApiResponse<FeedbackSummaryDto>.Ok(summary));
    }
}
