namespace WorkTicketApp.Models;

public class DashboardMetricsDto
{
    public int TotalTickets { get; set; }
    public int TicketsCreatedLast7Days { get; set; }
    public List<ChartDataPoint> TicketsByCostCentre { get; set; } = new();
    public List<ChartDataPoint> TicketsByOperator { get; set; } = new();
}