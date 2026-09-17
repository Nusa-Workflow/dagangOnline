using System;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

public interface IFeedbackService
{
    Task<bool> SubmitFeedbackAsync(FeedbackSubmissionDto dto, string? userId, CancellationToken cancellationToken = default);
    Task<FeedbackSummaryDto> GetFeedbackSummaryAsync(DateTime? since = null, CancellationToken cancellationToken = default);
}
