using dagangOnline.Data;
using dagangOnline.Domain;

namespace dagangOnline.Application.Services;

public class AuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string action, string entityName, string? entityId = null,
        string? performedByUserId = null, string? performedByUserName = null,
        string? details = null, string severity = "Info")
    {
        var entry = new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            PerformedByUserId = performedByUserId,
            PerformedByUserName = performedByUserName,
            Details = details,
            Severity = severity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task LogWarningAsync(string action, string entityName, string? entityId = null,
        string? performedByUserId = null, string? performedByUserName = null,
        string? details = null)
    {
        await LogAsync(action, entityName, entityId, performedByUserId, performedByUserName, details, "Warning");
    }

    public async Task LogCriticalAsync(string action, string entityName, string? entityId = null,
        string? performedByUserId = null, string? performedByUserName = null,
        string? details = null)
    {
        await LogAsync(action, entityName, entityId, performedByUserId, performedByUserName, details, "Critical");
    }
}