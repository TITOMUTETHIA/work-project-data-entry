using Microsoft.EntityFrameworkCore;
using WorkTicketApp.Data;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public class WorkTicketService : IWorkTicketService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditLogService _auditLogService;

    public WorkTicketService(
        IDbContextFactory<ApplicationDbContext> factory,
        IHttpContextAccessor httpContextAccessor,
        IAuditLogService auditLogService)
    {
        _factory = factory;
        _httpContextAccessor = httpContextAccessor;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<WorkTicket>> GetWorkTicketsAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true, DateTime? startDate = null, DateTime? endDate = null)
    {
        using var context = await _factory.CreateDbContextAsync();
        var query = context.WorkTickets.AsQueryable();

        var user = _httpContextAccessor.HttpContext?.User;
        var username = user?.Identity?.Name;

        if (user == null || !user.IsInRole("Admin"))
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(t => t.CreatedBy == username);
            }
            else
            {
                query = query.Where(t => false);
            }
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(t => 
                (t.TicketNumber != null && t.TicketNumber.ToLower().Contains(lowerSearchTerm)) ||
                (t.OperatorName != null && t.OperatorName.ToLower().Contains(lowerSearchTerm)) ||
                (t.Activity != null && t.Activity.ToLower().Contains(lowerSearchTerm)));
        }

        if (startDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= endDate.Value);
        }

        query = (sortBy?.ToLowerInvariant(), sortAscending) switch
        {
            ("ticketnumber", true) => query.OrderBy(t => t.TicketNumber),
            ("ticketnumber", false) => query.OrderByDescending(t => t.TicketNumber),
            ("costcentre", true) => query.OrderBy(t => t.CostCentre),
            ("costcentre", false) => query.OrderByDescending(t => t.CostCentre),
            ("activity", true) => query.OrderBy(t => t.Activity),
            ("activity", false) => query.OrderByDescending(t => t.Activity),
            ("operatorname", true) => query.OrderBy(t => t.OperatorName),
            ("operatorname", false) => query.OrderByDescending(t => t.OperatorName),
            ("dt", true) => query.OrderBy(t => t.DT),
            ("dt", false) => query.OrderByDescending(t => t.DT),
            ("startdatetime", true) => query.OrderBy(t => t.StartDateTime),
            ("startdatetime", false) => query.OrderByDescending(t => t.StartDateTime),
            ("enddatetime", true) => query.OrderBy(t => t.EndDateTime),
            ("enddatetime", false) => query.OrderByDescending(t => t.EndDateTime),
            _ => query.OrderByDescending(t => t.DT) // Default sort
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<WorkTicket> { Items = items, TotalCount = totalCount };
    }

    public async Task<WorkTicket> CreateWorkTicketAsync(WorkTicket ticket)
    {
        using var context = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        ticket.DT = now.ToString("o"); // ISO 8601 format
        ticket.CreatedAt = now;
        context.WorkTickets.Add(ticket);
        await context.SaveChangesAsync();
        return ticket;
    }

    public async Task<WorkTicket?> GetTicketByIdAsync(int id)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.WorkTickets.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task UpdateTicketAsync(WorkTicket ticket, string updatedBy)
    {
        using var context = await _factory.CreateDbContextAsync();
        var existing = await context.WorkTickets.FirstOrDefaultAsync(t => t.Id == ticket.Id);
        if (existing != null)
        {
            string details = "";

            // Detailed logging of changes
            if (existing.TicketNumber != ticket.TicketNumber)
                details += $"TicketNumber changed from '{existing.TicketNumber}' to '{ticket.TicketNumber}'. ";
            if (existing.CostCentre != ticket.CostCentre)
                details += $"CostCentre changed from '{existing.CostCentre}' to '{ticket.CostCentre}'. ";
            if (existing.Activity != ticket.Activity)
                details += $"Activity changed from '{existing.Activity}' to '{ticket.Activity}'. ";
            if (existing.OperatorName != ticket.OperatorName)
                details += $"OperatorName changed from '{existing.OperatorName}' to '{ticket.OperatorName}'. ";
            if (existing.NumOperators != ticket.NumOperators)
                details += $"NumOperators changed from '{existing.NumOperators}' to '{ticket.NumOperators}'. ";
            if (existing.StartCounter != ticket.StartCounter)
                details += $"StartCounter changed from '{existing.StartCounter}' to '{ticket.StartCounter}'. ";
            if (existing.EndCounter != ticket.EndCounter)
                details += $"EndCounter changed from '{existing.EndCounter}' to '{ticket.EndCounter}'. ";
            if (existing.StartDateTime != ticket.StartDateTime)
                details += $"StartDateTime changed from '{existing.StartDateTime}' to '{ticket.StartDateTime}'. ";
            if (existing.EndDateTime != ticket.EndDateTime)
                details += $"EndDateTime changed from '{existing.EndDateTime}' to '{ticket.EndDateTime}'. ";
            if (existing.QuantityIn != ticket.QuantityIn)
                details += $"QuantityIn changed from '{existing.QuantityIn}' to '{ticket.QuantityIn}'. ";
            if (existing.QuantityOut != ticket.QuantityOut)
                details += $"QuantityOut changed from '{existing.QuantityOut}' to '{ticket.QuantityOut}'. ";
            if (existing.MaterialUsed != ticket.MaterialUsed)
                details += $"MaterialUsed changed from '{existing.MaterialUsed}' to '{ticket.MaterialUsed}'. ";

            // Use SetValues for a more maintainable approach to copy properties.
            context.Entry(existing).CurrentValues.SetValues(ticket);

            // Explicitly set audit fields that should be controlled by the server.
            existing.UpdatedBy = updatedBy;
            existing.UpdatedAt = DateTime.UtcNow;
           await _auditLogService.LogAsync(updatedBy, "Work Ticket Updated", ticket.TicketNumber, details);
           // Ensure read-only fields are not modified by the incoming request.
           context.Entry(existing).Property(p => p.DT).IsModified = false;
           context.Entry(existing).Property(p => p.CreatedBy).IsModified = false;
           context.Entry(existing).Property(p => p.CreatedAt).IsModified = false;
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteTicketAsync(int id)
    {
        using var context = await _factory.CreateDbContextAsync();
        var ticket = await context.WorkTickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket != null)
        {
            context.WorkTickets.Remove(ticket);
            await context.SaveChangesAsync();
        }
    }

    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(string? username = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var query = context.WorkTickets.AsQueryable();

        if (!string.IsNullOrEmpty(username))
        {
            query = query.Where(t => t.CreatedBy == username);
        }

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // Combine simple counts into a single, more efficient database query.
        var statsTask = query.GroupBy(_ => 1).Select(g => new
        {
            TotalTickets = g.Count(),
            ActiveTickets = g.Count(t => t.EndDateTime == null || t.EndDateTime == ""),
            // Query against the DateTime column for better performance and type safety.
            TicketsCreatedLast7Days = g.Count(t => t.CreatedAt >= sevenDaysAgo)
        }).FirstOrDefaultAsync();

        // Execute chart data queries in parallel to reduce total wait time.
        var ticketsByCostCentreTask = query
            .Where(t => t.CostCentre != null)
            .GroupBy(t => t.CostCentre)
            .Select(g => new ChartDataPoint { Label = g.Key!, Value = g.Count() })
            .OrderByDescending(c => c.Value)
            .Take(10)
            .ToListAsync();

        var ticketsByOperatorTask = query
            .Where(t => t.OperatorName != null)
            .GroupBy(t => t.OperatorName)
            .Select(g => new ChartDataPoint { Label = g.Key!, Value = g.Count() })
            .OrderByDescending(c => c.Value)
            .Take(10)
            .ToListAsync();

        await Task.WhenAll(statsTask, ticketsByCostCentreTask, ticketsByOperatorTask);

        var stats = await statsTask ?? new { TotalTickets = 0, ActiveTickets = 0, TicketsCreatedLast7Days = 0 };
        var ticketsByCostCentre = await ticketsByCostCentreTask;
        var ticketsByOperator = await ticketsByOperatorTask;

        return new DashboardMetricsDto { TotalTickets = stats.TotalTickets, ActiveTickets = stats.ActiveTickets, TicketsCreatedLast7Days = stats.TicketsCreatedLast7Days, TicketsByCostCentre = ticketsByCostCentre, TicketsByOperator = ticketsByOperator };
    }
}