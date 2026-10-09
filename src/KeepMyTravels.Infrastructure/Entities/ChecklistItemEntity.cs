using SQLite;
using KeepMyTravels.Infrastructure;

namespace KeepMyTravels.Infrastructure.Entities;

[Table(DatabaseSchema.A5)]
internal sealed class ChecklistItemEntity
{
    [PrimaryKey]
    [Column(DatabaseSchema.B01)]
    public string Id { get; set; } = string.Empty;
    [Indexed]
    [Column(DatabaseSchema.B02)]
    public string TripId { get; set; } = string.Empty;
    [Column(DatabaseSchema.B42)]
    public string ListName { get; set; } = "General";
    [Column(DatabaseSchema.B43)]
    public string Text { get; set; } = string.Empty;
    [Column(DatabaseSchema.B44)]
    public bool IsCompleted { get; set; }
    [Column(DatabaseSchema.B27)]
    public int SortOrder { get; set; }
}
