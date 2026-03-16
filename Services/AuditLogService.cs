using Microsoft.EntityFrameworkCore;
using WorkTicketApp.Data;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public AuditLogService(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    public async Task LogAsync(string performedBy, string action, string? targetUser, string? details)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var logEntry = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            PerformedBy = performedBy,
            Action = action,
            TargetUser = targetUser,
            Details = details
        };
        context.Add(logEntry);
        await context.SaveChangesAsync();
    }

    public async Task<PagedResult<AuditLog>> GetLogsAsync(int pageNumber, int pageSize)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var query = context.Set<AuditLog>().OrderByDescending(l => l.Timestamp);
        var totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<AuditLog> { Items = items, TotalCount = totalCount };
    }
}