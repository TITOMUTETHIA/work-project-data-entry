using System.ComponentModel.DataAnnotations;
using WorkTicketApp.Validation;

namespace WorkTicketApp.Models;

public class WorkTicket
{
    public int Id { get; set; }

    [Required]
    public string? TicketNumber { get; set; }

    [Required]
    public string? CostCentre { get; set; }

    public string? Activity { get; set; }
    public string? OperatorName { get; set; }
    public int NumOperators { get; set; }
    public int StartCounter { get; set; }

    [GreaterThan(nameof(StartCounter), ErrorMessage = "End Counter must be greater than Start Counter.")]
    public int EndCounter { get; set; }
    public string? StartDateTime { get; set; }
    public string? EndDateTime { get; set; }
    public int QuantityIn { get; set; }
    public int QuantityOut { get; set; }
    public string? MaterialUsed { get; set; }
    public string? DT { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}