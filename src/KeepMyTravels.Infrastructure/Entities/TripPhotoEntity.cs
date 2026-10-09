using SQLite;
using KeepMyTravels.Infrastructure;

namespace KeepMyTravels.Infrastructure.Entities;

[Table(DatabaseSchema.A8)]
internal sealed class TripPhotoEntity
{
    [PrimaryKey]
    [Column(DatabaseSchema.B01)]
    public string Id { get; set; } = string.Empty;
    [Indexed]
    [Column(DatabaseSchema.B02)]
    public string TripId { get; set; } = string.Empty;
    [Column(DatabaseSchema.B56)]
    public string FilePath { get; set; } = string.Empty;
    [Column(DatabaseSchema.B37)]
    public string FileName { get; set; } = string.Empty;
    [Column(DatabaseSchema.B54)]
    public string Caption { get; set; } = string.Empty;
    [Column("c_capture_day")]
    public DateTime? CapturedDate { get; set; }
    [Column(DatabaseSchema.B57)]
    public DateTime TakenUtc { get; set; }
    [Column(DatabaseSchema.B41)]
    public DateTime AddedUtc { get; set; }
    [Column(DatabaseSchema.B27)]
    public int SortOrder { get; set; }
}
