using System.ComponentModel.DataAnnotations;

namespace WorkTicketApp.Models;

public class AuditLog
{
    [Key]
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? PerformedBy { get; set; }
    public string? Action { get; set; }
    public string? TargetUser { get; set; }
    public string? Details { get; set; }
}