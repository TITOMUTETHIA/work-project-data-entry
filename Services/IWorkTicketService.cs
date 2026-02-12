using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public interface IWorkTicketService
{
    Task<PagedResult<WorkTicket>> GetWorkTicketsAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true, DateTime? startDate = null, DateTime? endDate = null);
    Task<WorkTicket> CreateWorkTicketAsync(WorkTicket workTicket);
    Task<WorkTicket?> GetTicketByIdAsync(int id);
    Task UpdateTicketAsync(WorkTicket ticket);
    Task DeleteTicketAsync(int id);
}