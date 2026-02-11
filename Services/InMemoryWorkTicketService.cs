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
            _ => query.OrderByDescending(t => t.CreatedAt)
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

    public Task UpdateTicketAsync(WorkTicket ticket)
    {
        var existingTicket = _tickets.FirstOrDefault(t => t.Id == ticket.Id);
        if (existingTicket != null)
        {
            existingTicket.TicketNumber = ticket.TicketNumber;
            existingTicket.CostCentre = ticket.CostCentre;
            existingTicket.Activity = ticket.Activity;
            existingTicket.OperatorName = ticket.OperatorName;
            existingTicket.NumOperators = ticket.NumOperators;
            existingTicket.StartDateTime = ticket.StartDateTime;
            existingTicket.StartCounter = ticket.StartCounter;
            existingTicket.EndDateTime = ticket.EndDateTime;
            existingTicket.EndCounter = ticket.EndCounter;
            existingTicket.QuantityIn = ticket.QuantityIn;
            existingTicket.QuantityOut = ticket.QuantityOut;
            existingTicket.MaterialUsed = ticket.MaterialUsed;
            existingTicket.UpdatedAt = DateTime.UtcNow;
            existingTicket.CreatedBy = ticket.CreatedBy;
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
}