using System.ComponentModel.DataAnnotations;
using WorkTicketApp.Models.Validation;

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
    [IsValidDate(ErrorMessage = "Start Date Time must be a valid date.")]
    public string? StartDateTime { get; set; }
    public int StartCounter { get; set; }
    [IsValidDate(ErrorMessage = "End Date Time must be a valid date.")]
    [DateGreaterThan(nameof(StartDateTime), ErrorMessage = "End Date Time must be on or after Start Date Time.")]
    public string? EndDateTime { get; set; }
    public int EndCounter { get; set; }
    public int QuantityIn { get; set; }
    public int QuantityOut { get; set; }
    public string? MaterialUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}