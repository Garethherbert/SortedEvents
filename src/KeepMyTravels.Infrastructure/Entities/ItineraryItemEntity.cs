using SQLite;
using KeepMyTravels.Infrastructure;

namespace KeepMyTravels.Infrastructure.Entities;

[Table(DatabaseSchema.A2)]
internal sealed class ItineraryItemEntity
{
    [PrimaryKey]
    [Column(DatabaseSchema.B01)]
    public string Id { get; set; } = string.Empty;
    [Indexed]
    [Column(DatabaseSchema.B02)]
    public string TripId { get; set; } = string.Empty;
    [Column(DatabaseSchema.B19)]
    public int DayNumber { get; set; }
    [Column(DatabaseSchema.B20)]
    public DateTime? StartDateTime { get; set; }
    [Column(DatabaseSchema.B21)]
    public DateTime? EndDateTime { get; set; }
    [Column(DatabaseSchema.B22)]
    public string Title { get; set; } = string.Empty;
    [Column(DatabaseSchema.B23)]
    public string Location { get; set; } = string.Empty;
    [Column(DatabaseSchema.B24)]
    public string? Website { get; set; }
    [Column(DatabaseSchema.B10)]
    public string Notes { get; set; } = string.Empty;
    [Column(DatabaseSchema.B25)]
    public int Category { get; set; }
    [Column(DatabaseSchema.B26)]
    public string? BookingId { get; set; }
    [Column(DatabaseSchema.B27)]
    public int SortOrder { get; set; }
}
