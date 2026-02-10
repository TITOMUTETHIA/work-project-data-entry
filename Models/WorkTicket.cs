namespace WorkTicketApp.Models;

public class WorkTicket
{
    public int Id { get; set; }
    public string? TicketNumber { get; set; }
    public string? CostCentre { get; set; }
    public string? Activity { get; set; }
    public string? OperatorName { get; set; }
    public int NumOperators { get; set; }
    public DateTime StartDateTime { get; set; }
    public int StartCounter { get; set; }
    public DateTime EndDateTime { get; set; }
    public int EndCounter { get; set; }
    public int QuantityIn { get; set; }
    public int QuantityOut { get; set; }
    public string? MaterialUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
