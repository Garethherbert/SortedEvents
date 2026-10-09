namespace KeepMyTravels.Core.Models;

public sealed class PreTravelItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public string Category { get; set; } = "General";
    public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime DueDate { get; set; } = DateTime.Today;
    public string Notes { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
