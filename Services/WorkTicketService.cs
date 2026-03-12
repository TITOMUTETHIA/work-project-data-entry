using Microsoft.EntityFrameworkCore;
using WorkTicketApp.Data;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public class WorkTicketService : IWorkTicketService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public WorkTicketService(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<PagedResult<WorkTicket>> GetWorkTicketsAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true, DateTime? startDate = null, DateTime? endDate = null)
    {
        using var context = await _factory.CreateDbContextAsync();
        var query = context.WorkTickets.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(t => t.StartDateTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.StartDateTime < endDate.Value.AddDays(1));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(t => 
                (t.TicketNumber != null && t.TicketNumber.ToLower().Contains(lowerSearchTerm)) ||
                (t.OperatorName != null && t.OperatorName.ToLower().Contains(lowerSearchTerm)) ||
                (t.Activity != null && t.Activity.ToLower().Contains(lowerSearchTerm)));
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
            ("startdatetime", true) => query.OrderBy(t => t.StartDateTime),
            ("startdatetime", false) => query.OrderByDescending(t => t.StartDateTime),
            _ => query.OrderByDescending(t => t.CreatedAt) // Default sort
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<WorkTicket> { Items = items, TotalCount = totalCount };
    }

    public async Task<WorkTicket> CreateWorkTicketAsync(WorkTicket ticket)
    {
        using var context = await _factory.CreateDbContextAsync();
        ticket.CreatedAt = DateTime.UtcNow;
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
            // Use SetValues for a more maintainable approach to copy properties.
            context.Entry(existing).CurrentValues.SetValues(ticket);

            // Explicitly set audit fields that should be controlled by the server.
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = updatedBy;

            // Ensure read-only fields are not modified by the incoming request.
            // This prevents over-posting vulnerabilities.
            context.Entry(existing).Property(p => p.CreatedAt).IsModified = false;
            context.Entry(existing).Property(p => p.CreatedBy).IsModified = false;

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

    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var totalTickets = await context.WorkTickets.CountAsync();
        
        var activeTickets = await context.WorkTickets
            .CountAsync(t => t.StartDateTime != null && !t.EndDateTime.HasValue);

        var ticketsCreatedLast7Days = await context.WorkTickets
            .CountAsync(t => t.CreatedAt >= sevenDaysAgo);

        var ticketsByCostCentre = await context.WorkTickets
            .Where(t => t.CostCentre != null)
            .GroupBy(t => t.CostCentre)
            .Select(g => new ChartDataPoint { Label = g.Key!, Value = g.Count() })
            .OrderByDescending(c => c.Value)
            .Take(10) // Take top 10 for clarity
            .ToListAsync();

        var ticketsByOperator = await context.WorkTickets
            .Where(t => t.OperatorName != null)
            .GroupBy(t => t.OperatorName)
            .Select(g => new ChartDataPoint { Label = g.Key!, Value = g.Count() })
            .OrderByDescending(c => c.Value)
            .Take(10) // Take top 10 for clarity
            .ToListAsync();

        return new DashboardMetricsDto { TotalTickets = totalTickets, ActiveTickets = activeTickets, TicketsCreatedLast7Days = ticketsCreatedLast7Days, TicketsByCostCentre = ticketsByCostCentre, TicketsByOperator = ticketsByOperator };
    }
}