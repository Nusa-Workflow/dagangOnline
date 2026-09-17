using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Data;
using dagangOnline.Domain.Chat;

namespace dagangOnline.Application.Services.Feedback;

public class FeedbackService : IFeedbackService
{
    private readonly ApplicationDbContext _db;

    public FeedbackService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> SubmitFeedbackAsync(FeedbackSubmissionDto dto, string? userId, CancellationToken cancellationToken = default)
    {
        var feedback = new ConversationFeedback
        {
            ConversationId = dto.ConversationId,
            MessageId = dto.MessageId,
            UserId = userId,
            FeedbackType = dto.FeedbackType,
            ReasonCode = dto.Reason,
            CustomReason = dto.CustomComment,
            Language = dto.Language ?? "id-ID",
            CreatedAt = DateTime.UtcNow
        };

        _db.ConversationFeedbacks.Add(feedback);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<FeedbackSummaryDto> GetFeedbackSummaryAsync(DateTime? since = null, CancellationToken cancellationToken = default)
    {
        var query = _db.ConversationFeedbacks.AsQueryable();

        if (since.HasValue)
        {
            query = query.Where(f => f.CreatedAt >= since.Value);
        }

        var list = await query.ToListAsync(cancellationToken);

        int likes = list.Count(f => f.FeedbackType.Equals("Like", StringComparison.OrdinalIgnoreCase));
        int unlikes = list.Count(f => f.FeedbackType.Equals("Unlike", StringComparison.OrdinalIgnoreCase));

        var topReasons = list
            .Where(f => f.FeedbackType.Equals("Unlike", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(f.ReasonCode))
            .GroupBy(f => f.ReasonCode!)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        return new FeedbackSummaryDto
        {
            TotalLikes = likes,
            TotalUnlikes = unlikes,
            TopUnlikeReasons = topReasons
        };
    }
}
