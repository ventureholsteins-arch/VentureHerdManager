namespace VentureHerdManager.Api.DTOs;

public class DashboardSummaryDto
{
    public int TotalAnimals { get; set; }
    public int Milking { get; set; }
    public int Dry { get; set; }
    public int Heifers { get; set; }
    public int Calves { get; set; }
    public int Bulls { get; set; }
    public int Open { get; set; }
    public int Bred { get; set; }
    public int DueSoon { get; set; }
}