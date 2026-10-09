namespace KeepMyTravels.Core.Models;

public sealed class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Guid? BookingId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string Category { get; set; } = "Other";
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "GBP";
    /// <summary>Multiplier from this expense currency to the trip base currency.</summary>
    public decimal ExchangeRateToBase { get; set; } = 1m;
    public decimal AmountInBase => decimal.Round(Amount * (ExchangeRateToBase <= 0 ? 1m : ExchangeRateToBase), 2);
    public string DisplayAmount => $"{CurrencyCode} {Amount:N2}";
    public string Notes { get; set; } = string.Empty;
}
