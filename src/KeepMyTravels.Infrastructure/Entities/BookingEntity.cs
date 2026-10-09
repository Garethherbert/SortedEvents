using SQLite;
using KeepMyTravels.Infrastructure;

namespace KeepMyTravels.Infrastructure.Entities;

[Table(DatabaseSchema.A3)]
internal sealed class BookingEntity
{
    [PrimaryKey]
    [Column(DatabaseSchema.B01)]
    public string Id { get; set; } = string.Empty;
    [Indexed]
    [Column(DatabaseSchema.B02)]
    public string TripId { get; set; } = string.Empty;
    [Column(DatabaseSchema.B52)]
    public int Type { get; set; }
    [Column(DatabaseSchema.B03)]
    public string Name { get; set; } = string.Empty;
    [Column(DatabaseSchema.B28)]
    public string Provider { get; set; } = string.Empty;
    [Column(DatabaseSchema.B29)]
    public string Reference { get; set; } = string.Empty;
    [Column(DatabaseSchema.B30)]
    public string Address { get; set; } = string.Empty;
    [Column(DatabaseSchema.B31)]
    public string Phone { get; set; } = string.Empty;
    [Column(DatabaseSchema.B24)]
    public string Website { get; set; } = string.Empty;
    [Column(DatabaseSchema.B59)]
    public string GoogleWalletUrl { get; set; } = string.Empty;
    [Column(DatabaseSchema.B60)]
    public string SamsungWalletUrl { get; set; } = string.Empty;
    [Column(DatabaseSchema.B32)]
    public DateTime? CheckIn { get; set; }
    [Column(DatabaseSchema.B33)]
    public DateTime? CheckOut { get; set; }
    [Column(DatabaseSchema.B34)]
    public decimal TotalCost { get; set; }
    [Column(DatabaseSchema.B35)]
    public decimal AmountPaid { get; set; }
    [Column(DatabaseSchema.B08)]
    public string CurrencyCode { get; set; } = "GBP";
    [Column(DatabaseSchema.B36)]
    public decimal ExchangeRateToBase { get; set; } = 1m;
    [Column(DatabaseSchema.B09)]
    public int Status { get; set; }
    [Column(DatabaseSchema.B10)]
    public string Notes { get; set; } = string.Empty;
}
