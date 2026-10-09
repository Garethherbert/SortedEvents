namespace KeepMyTravels.Core.Models;

public sealed class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public string ListName { get; set; } = "General";
    public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int SortOrder { get; set; }
}
