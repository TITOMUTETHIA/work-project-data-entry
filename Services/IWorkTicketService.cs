using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public interface IWorkTicketService
{
    Task<List<WorkTicket>> GetAllTicketsAsync();
    Task<WorkTicket?> GetTicketByNumberAsync(string ticketNumber);
    Task<WorkTicket> CreateTicketAsync(WorkTicket ticket);
    Task<WorkTicket> UpdateTicketAsync(WorkTicket ticket);
    Task<bool> DeleteTicketAsync(int id);
}
