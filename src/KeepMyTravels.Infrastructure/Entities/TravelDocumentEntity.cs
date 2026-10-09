using SQLite;
using KeepMyTravels.Infrastructure;

namespace KeepMyTravels.Infrastructure.Entities;

[Table(DatabaseSchema.A6)]
internal sealed class TravelDocumentEntity
{
    [PrimaryKey]
    [Column(DatabaseSchema.B01)]
    public string Id { get; set; } = string.Empty;
    [Indexed]
    [Column(DatabaseSchema.B02)]
    public string TripId { get; set; } = string.Empty;
    [Column(DatabaseSchema.B22)]
    public string Title { get; set; } = string.Empty;
    [Column(DatabaseSchema.B25)]
    public string Category { get; set; } = "Document";
    [Column(DatabaseSchema.B37)]
    public string FileName { get; set; } = string.Empty;
    [Column(DatabaseSchema.B38)]
    public string RelativePath { get; set; } = string.Empty;
    [Column(DatabaseSchema.B39)]
    public string MimeType { get; set; } = "application/octet-stream";
    [Column(DatabaseSchema.B40)]
    public long SizeBytes { get; set; }
    [Column(DatabaseSchema.B41)]
    public DateTime AddedUtc { get; set; }
    [Column(DatabaseSchema.B10)]
    public string Notes { get; set; } = string.Empty;
}
