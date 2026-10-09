using SQLite;
using KeepMyTravels.Infrastructure;

namespace KeepMyTravels.Infrastructure.Entities;

[Table(DatabaseSchema.A4)]
internal sealed class ExpenseEntity
{
    [PrimaryKey]
    [Column(DatabaseSchema.B01)]
    public string Id { get; set; } = string.Empty;
    [Indexed]
    [Column(DatabaseSchema.B02)]
    public string TripId { get; set; } = string.Empty;
    [Column(DatabaseSchema.B26)]
    public string? BookingId { get; set; }
    [Column(DatabaseSchema.B53)]
    public DateTime Date { get; set; }
    [Column(DatabaseSchema.B25)]
    public string Category { get; set; } = "Other";
    [Column(DatabaseSchema.B54)]
    public string Description { get; set; } = string.Empty;
    [Column(DatabaseSchema.B55)]
    public decimal Amount { get; set; }
    [Column(DatabaseSchema.B08)]
    public string CurrencyCode { get; set; } = "GBP";
    [Column(DatabaseSchema.B36)]
    public decimal ExchangeRateToBase { get; set; } = 1m;
    [Column(DatabaseSchema.B10)]
    public string Notes { get; set; } = string.Empty;
}
