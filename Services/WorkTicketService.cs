using Microsoft.EntityFrameworkCore;
using WorkTicketApp.Data;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services;

public sealed class WorkTicketService(WorkTicketContext context) : IWorkTicketService
{
    public async Task<List<WorkTicket>> GetAllTicketsAsync()
    {
        return await context.WorkTickets
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<WorkTicket?> GetTicketByNumberAsync(string ticketNumber)
    {
        return await context.WorkTickets
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);
    }

    public async Task<WorkTicket> CreateTicketAsync(WorkTicket ticket)
    {
        context.WorkTickets.Add(ticket);
        await context.SaveChangesAsync();
        return ticket;
    }

    public async Task<WorkTicket> UpdateTicketAsync(WorkTicket ticket)
    {
        ticket.UpdatedAt = DateTime.UtcNow;
        context.WorkTickets.Update(ticket);
        await context.SaveChangesAsync();
        return ticket;
    }

    public async Task<bool> DeleteTicketAsync(int id)
    {
        var ticket = await context.WorkTickets.FindAsync(id);
        if (ticket is null)
            return false;

        context.WorkTickets.Remove(ticket);
        await context.SaveChangesAsync();
        return true;
    }
}
