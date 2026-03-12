using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkTicketApp.Models;
using WorkTicketApp.Services;

namespace WorkTicketApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Secure all endpoints in this controller
    public class WorkTicketsController : ControllerBase
    {
        private readonly IWorkTicketService _ticketService;

        public WorkTicketsController(IWorkTicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<WorkTicket>>> GetWorkTickets([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _ticketService.GetWorkTicketsAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<WorkTicket>> GetTicketById(int id)
        {
            var ticket = await _ticketService.GetTicketByIdAsync(id);
            return ticket is not null ? Ok(ticket) : NotFound();
        }
    }
}