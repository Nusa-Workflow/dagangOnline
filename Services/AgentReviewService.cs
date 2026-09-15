using dagangOnline.Application.Services;
using dagangOnline.Data;
using dagangOnline.Domain;
using dagangOnline.Domain.Agents;
using dagangOnline.Models.Agents;
using Microsoft.EntityFrameworkCore;

namespace dagangOnline.Services;

public class AgentReviewService
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;

    public AgentReviewService(ApplicationDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<List<ReviewQueueItemDto>> GetPendingTasksAsync()
    {
        return await _context.ReviewTasks
            .Include(t => t.Product)
            .Where(t => t.Status == ReviewTaskStatus.Pending || t.Status == ReviewTaskStatus.InReview)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new ReviewQueueItemDto
            {
                TaskId = t.Id,
                ProductId = t.ProductId,
                ProductName = t.Product != null ? t.Product.Name : "Produk Tidak Ditemukan",
                ProductSummary = t.Product != null ? t.Product.Summary : string.Empty,
                ProductDescription = t.Product != null ? t.Product.Description : string.Empty,
                Price = t.Product != null ? t.Product.Price : 0,
                Category = t.Product != null ? t.Product.Category : ProductCategory.General,
                OwnerName = t.Product != null ? t.Product.OwnerName : null,
                OwnerId = t.Product != null ? t.Product.OwnerId : null,
                SubmittedAt = t.CreatedAt,
                TaskStatus = t.Status
            })
            .ToListAsync();
    }

    public async Task<ReviewTask?> GetTaskByIdAsync(Guid taskId)
    {
        return await _context.ReviewTasks
            .Include(t => t.Product)
            .FirstOrDefaultAsync(t => t.Id == taskId);
    }

    public async Task<ReviewTask> CreateReviewTaskForProductAsync(Product product)
    {
        var task = new ReviewTask
        {
            ProductId = product.Id,
            Product = product,
            Status = ReviewTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ReviewTasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<bool> ApproveProductAsync(Guid taskId, string reviewerId, string reviewerName, string? notes)
    {
        var task = await _context.ReviewTasks
            .Include(t => t.Product)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null || task.Product == null)
        {
            return false;
        }

        task.Status = ReviewTaskStatus.Approved;
        task.ReviewerId = reviewerId;
        task.ReviewerName = reviewerName;
        task.CompletedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        task.Product.Status = PublicationStatus.Published;
        task.Product.UpdatedAt = DateTime.UtcNow;

        var decision = new ModerationDecision
        {
            ReviewTaskId = task.Id,
            ProductId = task.ProductId,
            ReviewerId = reviewerId,
            ReviewerName = reviewerName,
            Decision = "Approved",
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ModerationDecisions.Add(decision);

        // Notify Mitra
        if (!string.IsNullOrWhiteSpace(task.Product.OwnerId))
        {
            var owner = await _context.Users.FindAsync(task.Product.OwnerId);
            if (owner != null)
            {
                var notif = new Notification
                {
                    Title = "Produk Anda Disetujui!",
                    Message = $"Produk '{task.Product.Name}' telah disetujui oleh moderator ({reviewerName}) dan kini telah tayang di katalog platform.",
                    Type = "Success",
                    RecipientEmail = owner.Email,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notif);
            }
        }

        await _auditLogService.LogAsync(
            action: "ApproveProduct",
            entityName: "Product",
            entityId: task.Product.Id.ToString(),
            performedByUserName: reviewerName,
            details: $"Produk '{task.Product.Name}' disetujui oleh {reviewerName}. Catatan: {notes ?? "-"}"
        );

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectProductAsync(Guid taskId, string reviewerId, string reviewerName, string reason, string? notes)
    {
        var task = await _context.ReviewTasks
            .Include(t => t.Product)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null || task.Product == null)
        {
            return false;
        }

        task.Status = ReviewTaskStatus.Rejected;
        task.ReviewerId = reviewerId;
        task.ReviewerName = reviewerName;
        task.CompletedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        task.Product.Status = PublicationStatus.Rejected;
        task.Product.UpdatedAt = DateTime.UtcNow;

        var decision = new ModerationDecision
        {
            ReviewTaskId = task.Id,
            ProductId = task.ProductId,
            ReviewerId = reviewerId,
            ReviewerName = reviewerName,
            Decision = "Rejected",
            Reason = reason,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ModerationDecisions.Add(decision);

        // Notify Mitra with rejection reason
        if (!string.IsNullOrWhiteSpace(task.Product.OwnerId))
        {
            var owner = await _context.Users.FindAsync(task.Product.OwnerId);
            if (owner != null)
            {
                var notif = new Notification
                {
                    Title = "Perlu Revisi Produk",
                    Message = $"Pengajuan produk '{task.Product.Name}' ditolak oleh moderator ({reviewerName}). Alasan: {reason}. Silakan perbaiki dan ajukan ulang.",
                    Type = "Warning",
                    RecipientEmail = owner.Email,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notif);
            }
        }

        await _auditLogService.LogWarningAsync(
            action: "RejectProduct",
            entityName: "Product",
            entityId: task.Product.Id.ToString(),
            performedByUserName: reviewerName,
            details: $"Produk '{task.Product.Name}' ditolak oleh {reviewerName}. Alasan: {reason}. Catatan: {notes ?? "-"}"
        );

        await _context.SaveChangesAsync();
        return true;
    }
}
