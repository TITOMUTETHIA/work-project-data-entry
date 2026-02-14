using Microsoft.EntityFrameworkCore;
using WorkTicketApp.Data;
using WorkTicketApp.Models;

namespace WorkTicketApp.Services
{
    public class WorkTicketService : IWorkTicketService
    {
        private readonly ApplicationDbContext _context;

        public WorkTicketService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateWorkTicketAsync(WorkTicket ticket)
        {
            ticket.CreatedAt = DateTime.UtcNow;
            _context.WorkTickets.Add(ticket);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<WorkTicket>> GetWorkTicketsAsync(int page, int pageSize, string? searchTerm, string? sortBy, bool sortAscending, DateTime? startDate = null, DateTime? endDate = null, DateTime? updatedStartDate = null, DateTime? updatedEndDate = null)
        {
            var query = _context.WorkTickets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(t => 
                    (t.TicketNumber != null && t.TicketNumber.Contains(searchTerm)) || 
                    (t.Activity != null && t.Activity.Contains(searchTerm)) || 
                    (t.OperatorName != null && t.OperatorName.Contains(searchTerm)));
            }

            if (startDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                // Include the entire end day (up to 23:59:59)
                var end = endDate.Value.Date.AddDays(1);
                query = query.Where(t => t.CreatedAt < end);
            }

            if (updatedStartDate.HasValue)
            {
                query = query.Where(t => t.UpdatedAt >= updatedStartDate.Value);
            }

            if (updatedEndDate.HasValue)
            {
                // Include the entire end day (up to 23:59:59)
                var end = updatedEndDate.Value.Date.AddDays(1);
                query = query.Where(t => t.UpdatedAt < end);
            }

            query = (sortBy?.ToLowerInvariant(), sortAscending) switch
            {
                ("ticketnumber", true) => query.OrderBy(t => t.TicketNumber),
                ("ticketnumber", false) => query.OrderByDescending(t => t.TicketNumber),
                ("createdat", true) => query.OrderBy(t => t.CreatedAt),
                ("createdat", false) => query.OrderByDescending(t => t.CreatedAt),
                _ => query.OrderByDescending(t => t.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<WorkTicket> { Items = items, TotalCount = totalCount };
        }

        public async Task<WorkTicket?> GetTicketByIdAsync(int id)
        {
            return await _context.WorkTickets.FindAsync(id);
        }

        public async Task UpdateTicketAsync(WorkTicket ticket)
        {
            ticket.UpdatedAt = DateTime.UtcNow;
            _context.WorkTickets.Update(ticket);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTicketAsync(int id)
        {
            var ticket = await _context.WorkTickets.FindAsync(id);
            if (ticket != null)
            {
                _context.WorkTickets.Remove(ticket);
                await _context.SaveChangesAsync();
            }
        }
    }
}