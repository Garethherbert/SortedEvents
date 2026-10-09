using SQLite;
using KeepMyTravels.Infrastructure;

namespace KeepMyTravels.Infrastructure.Entities;

[Table(DatabaseSchema.A7)]
internal sealed class TravelWalletItemEntity
{
    [PrimaryKey]
    [Column(DatabaseSchema.B01)]
    public string Id { get; set; } = string.Empty;
    [Indexed]
    [Column(DatabaseSchema.B02)]
    public string TripId { get; set; } = string.Empty;
    [Column(DatabaseSchema.B25)]
    public int Category { get; set; }
    [Column(DatabaseSchema.B22)]
    public string Title { get; set; } = string.Empty;
    [Column(DatabaseSchema.B29)]
    public string Reference { get; set; } = string.Empty;
    [Column(DatabaseSchema.B45)]
    public string HolderName { get; set; } = string.Empty;
    [Column(DatabaseSchema.B46)]
    public DateTime? ValidFrom { get; set; }
    [Column(DatabaseSchema.B47)]
    public DateTime? ValidUntil { get; set; }
    [Column(DatabaseSchema.B48)]
    public string ContactName { get; set; } = string.Empty;
    [Column(DatabaseSchema.B49)]
    public string ContactPhone { get; set; } = string.Empty;
    [Column(DatabaseSchema.B50)]
    public string QrPayload { get; set; } = string.Empty;
    [Column(DatabaseSchema.B10)]
    public string Notes { get; set; } = string.Empty;
    [Column(DatabaseSchema.B51)]
    public string? DocumentId { get; set; }
    [Column(DatabaseSchema.B59)]
    public string GoogleWalletUrl { get; set; } = string.Empty;
    [Column(DatabaseSchema.B60)]
    public string SamsungWalletUrl { get; set; } = string.Empty;
}
