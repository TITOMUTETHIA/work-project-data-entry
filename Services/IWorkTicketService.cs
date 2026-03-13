using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public interface IWorkTicketService
{
    Task<PagedResult<WorkTicket>> GetWorkTicketsAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortAscending = true);
    Task<WorkTicket> CreateWorkTicketAsync(WorkTicket workTicket);
    Task<WorkTicket?> GetTicketByIdAsync(int id);
    Task UpdateTicketAsync(WorkTicket ticket, string updatedBy);
    Task DeleteTicketAsync(int id);
    Task<DashboardMetricsDto> GetDashboardMetricsAsync();
}