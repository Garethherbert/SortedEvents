using KeepMyTravels.Core.Enums;

namespace KeepMyTravels.Core.Models;

public sealed class ItineraryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public int DayNumber { get; set; } = 1;
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public ItineraryCategory Category { get; set; } = ItineraryCategory.General;
    public Guid? BookingId { get; set; }
    public int SortOrder { get; set; }
}
