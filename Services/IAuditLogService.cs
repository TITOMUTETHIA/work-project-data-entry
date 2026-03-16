using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public interface IAuditLogService
{
    Task LogAsync(string performedBy, string action, string? targetUser, string? details);
    Task<PagedResult<AuditLog>> GetLogsAsync(int pageNumber, int pageSize);
}