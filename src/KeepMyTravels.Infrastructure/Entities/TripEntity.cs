using SQLite;
using KeepMyTravels.Infrastructure;

namespace KeepMyTravels.Infrastructure.Entities;

[Table(DatabaseSchema.A1)]
internal sealed class TripEntity
{
    [PrimaryKey]
    [Column(DatabaseSchema.B01)]
    public string Id { get; set; } = string.Empty;
    [Column("c_shared_origin")]
    public string CollaborationId { get; set; } = string.Empty;
    [Indexed]
    [Column(DatabaseSchema.B03)]
    public string Name { get; set; } = string.Empty;
    [Column(DatabaseSchema.B04)]
    public string Destination { get; set; } = string.Empty;
    [Column(DatabaseSchema.B05)]
    public DateTime StartDate { get; set; }
    [Column(DatabaseSchema.B06)]
    public DateTime EndDate { get; set; }
    [Column(DatabaseSchema.B07)]
    public decimal Budget { get; set; }
    [Column(DatabaseSchema.B08)]
    public string CurrencyCode { get; set; } = "GBP";
    [Column(DatabaseSchema.B09)]
    public int Status { get; set; }
    [Column(DatabaseSchema.B10)]
    public string Notes { get; set; } = string.Empty;
    [Column(DatabaseSchema.B11)]
    public string BackgroundImagePath { get; set; } = string.Empty;
    [Column(DatabaseSchema.B12)]
    public string BackgroundImageSourceUrl { get; set; } = string.Empty;
    [Column(DatabaseSchema.B13)]
    public string BackgroundImageCreator { get; set; } = string.Empty;
    [Column(DatabaseSchema.B14)]
    public string BackgroundImageLicense { get; set; } = string.Empty;
    [Column(DatabaseSchema.B15)]
    public string BackgroundImageLicenseUrl { get; set; } = string.Empty;
    [Column(DatabaseSchema.B16)]
    public string BackgroundImageProvider { get; set; } = string.Empty;
    [Column(DatabaseSchema.B17)]
    public DateTime CreatedUtc { get; set; }
    [Column(DatabaseSchema.B18)]
    public DateTime UpdatedUtc { get; set; }
}
