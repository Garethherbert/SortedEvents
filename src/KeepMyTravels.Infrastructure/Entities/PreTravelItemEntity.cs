using SQLite;
using KeepMyTravels.Infrastructure;

namespace KeepMyTravels.Infrastructure.Entities;

[Table(DatabaseSchema.A9)]
internal sealed class PreTravelItemEntity
{
    [PrimaryKey]
    [Column(DatabaseSchema.B01)]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    [Column(DatabaseSchema.B02)]
    public string TripId { get; set; } = string.Empty;

    [Column(DatabaseSchema.B25)]
    public string Category { get; set; } = "General";

    [Column(DatabaseSchema.B43)]
    public string Text { get; set; } = string.Empty;

    [Column(DatabaseSchema.B44)]
    public bool IsCompleted { get; set; }

    [Column(DatabaseSchema.B58)]
    public DateTime DueDate { get; set; }

    [Column(DatabaseSchema.B10)]
    public string Notes { get; set; } = string.Empty;

    [Column(DatabaseSchema.B27)]
    public int SortOrder { get; set; }
}
