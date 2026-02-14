using WorkTicketApp.Models;

namespace WorkTicketApp.Services
{
    public interface IWorkTicketService
    {
        Task CreateWorkTicketAsync(WorkTicket ticket);
        Task<PagedResult<WorkTicket>> GetWorkTicketsAsync(int page, int pageSize, string? searchTerm, string? sortBy, bool sortAscending, DateTime? startDate = null, DateTime? endDate = null, DateTime? updatedStartDate = null, DateTime? updatedEndDate = null);
        Task<WorkTicket?> GetTicketByIdAsync(int id);
        Task UpdateTicketAsync(WorkTicket ticket);
        Task DeleteTicketAsync(int id);
    }
}