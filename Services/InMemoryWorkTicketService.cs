using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public class InMemoryWorkTicketService : IWorkTicketService
{
    private readonly List<WorkTicket> _tickets = new();
    private int _nextId = 1;

    public Task<PagedResult<WorkTicket>> GetWorkTicketsAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _tickets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t => 
                (t.TicketNumber != null && t.TicketNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (t.OperatorName != null && t.OperatorName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (t.Activity != null && t.Activity.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        // Date filtering
        if (startDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= endDate.Value);
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
            ("dt", true) => query.OrderBy(t => t.DT),
            ("dt", false) => query.OrderByDescending(t => t.DT),
            _ => query.OrderByDescending(t => t.DT) // Default sort
        };

        var totalCount = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedResult<WorkTicket> { Items = items, TotalCount = totalCount });
    }

    public Task<WorkTicket> CreateWorkTicketAsync(WorkTicket ticket)
    {
        ticket.Id = _nextId++;
        var now = DateTime.UtcNow;
        ticket.DT = now.ToString("o");
        ticket.CreatedAt = now;
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
                ticket.DT = existingTicket.DT;
                ticket.CreatedBy = existingTicket.CreatedBy;
                ticket.CreatedAt = existingTicket.CreatedAt;
                ticket.UpdatedBy = updatedBy;
                ticket.UpdatedAt = DateTime.UtcNow;

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

    public Task<DashboardMetricsDto> GetDashboardMetricsAsync(string? username = null)
    {
        var query = _tickets.AsQueryable();

        if (!string.IsNullOrEmpty(username))
        {
            query = query.Where(t => t.CreatedBy == username);
        }

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var totalTickets = query.Count();
        var activeTickets = query.Count(t => string.IsNullOrEmpty(t.EndDateTime));

        var sevenDaysAgoString = sevenDaysAgo.ToString("o");
        var ticketsCreatedLast7Days = query.Count(t => t.DT != null && t.DT.CompareTo(sevenDaysAgoString) >= 0);

        var ticketsByCostCentre = query
            .Where(t => !string.IsNullOrEmpty(t.CostCentre))
            .GroupBy(t => t.CostCentre)
            .Select(g => new ChartDataPoint { Label = g.Key!, Value = g.Count() })
            .OrderByDescending(c => c.Value)
            .Take(10)
            .ToList();

        var ticketsByOperator = query
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