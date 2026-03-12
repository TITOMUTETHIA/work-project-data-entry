using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public class InMemoryWorkTicketService : IWorkTicketService
{
    private readonly List<WorkTicket> _tickets = new();
    private int _nextId = 1;

    public Task<PagedResult<WorkTicket>> GetWorkTicketsAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _tickets.AsQueryable();

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
            query = query.Where(t => 
                (t.TicketNumber != null && t.TicketNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (t.OperatorName != null && t.OperatorName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (t.Activity != null && t.Activity.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply sorting
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

        var totalCount = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedResult<WorkTicket> { Items = items, TotalCount = totalCount });
    }

    public Task<WorkTicket> CreateWorkTicketAsync(WorkTicket ticket)
    {
        ticket.Id = _nextId++;
        ticket.CreatedAt = DateTime.UtcNow;
        _tickets.Add(ticket);
        return Task.FromResult(ticket);
    }

    public Task<WorkTicket?> GetTicketByIdAsync(int id)
    {
        var ticket = _tickets.FirstOrDefault(t => t.Id == id);
        return Task.FromResult(ticket);
    }

    public Task UpdateTicketAsync(WorkTicket ticket, string updatedBy)
    {
        var existingTicket = _tickets.FirstOrDefault(t => t.Id == ticket.Id);
        if (existingTicket != null)
        {
            // To avoid manual property copying and improve maintainability,
            // we replace the object in the list.
            var index = _tickets.IndexOf(existingTicket);
            if (index != -1)
            {
                // Ensure read-only and server-controlled fields are preserved/set correctly.
                ticket.Id = existingTicket.Id;
                ticket.CreatedAt = existingTicket.CreatedAt;
                ticket.CreatedBy = existingTicket.CreatedBy;
                ticket.UpdatedAt = DateTime.UtcNow;
                ticket.UpdatedBy = updatedBy;

                _tickets[index] = ticket;
            }
        }
        return Task.CompletedTask;
    }

    public Task DeleteTicketAsync(int id)
    {
        var ticket = _tickets.FirstOrDefault(t => t.Id == id);
        if (ticket != null)
        {
            _tickets.Remove(ticket);
        }
        return Task.CompletedTask;
    }

    public Task<DashboardMetricsDto> GetDashboardMetricsAsync()
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var totalTickets = _tickets.Count;
        
        var activeTickets = _tickets
            .Count(t => t.StartDateTime.HasValue && !t.EndDateTime.HasValue);

        var ticketsCreatedLast7Days = _tickets
            .Count(t => t.CreatedAt >= sevenDaysAgo);

        var ticketsByCostCentre = _tickets
            .Where(t => !string.IsNullOrEmpty(t.CostCentre))
            .GroupBy(t => t.CostCentre)
            .Select(g => new ChartDataPoint { Label = g.Key!, Value = g.Count() })
            .OrderByDescending(c => c.Value)
            .Take(10)
            .ToList();

        var ticketsByOperator = _tickets
            .Where(t => !string.IsNullOrEmpty(t.OperatorName))
            .GroupBy(t => t.OperatorName)
            .Select(g => new ChartDataPoint { Label = g.Key!, Value = g.Count() })
            .OrderByDescending(c => c.Value)
            .Take(10)
            .ToList();

        var metrics = new DashboardMetricsDto { TotalTickets = totalTickets, ActiveTickets = activeTickets, TicketsCreatedLast7Days = ticketsCreatedLast7Days, TicketsByCostCentre = ticketsByCostCentre, TicketsByOperator = ticketsByOperator };

        return Task.FromResult(metrics);
    }
}