using KeepMyTravels.Core.Enums;

namespace KeepMyTravels.Core.Models;

public sealed class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public BookingType Type { get; set; } = BookingType.Other;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string GoogleWalletUrl { get; set; } = string.Empty;
    public string SamsungWalletUrl { get; set; } = string.Empty;
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AmountPaid { get; set; }
    public string CurrencyCode { get; set; } = "GBP";
    /// <summary>Multiplier from this booking currency to the trip base currency. Example: 1 EUR = 0.86 GBP means 0.86.</summary>
    public decimal ExchangeRateToBase { get; set; } = 1m;
    public BookingStatus Status { get; set; } = BookingStatus.ToBook;
    public string Notes { get; set; } = string.Empty;

    public decimal Outstanding => Math.Max(0, TotalCost - AmountPaid);
    public decimal TotalCostInBase => decimal.Round(TotalCost * (ExchangeRateToBase <= 0 ? 1m : ExchangeRateToBase), 2);
    public decimal AmountPaidInBase => decimal.Round(AmountPaid * (ExchangeRateToBase <= 0 ? 1m : ExchangeRateToBase), 2);
    public decimal OutstandingInBase => decimal.Round(Outstanding * (ExchangeRateToBase <= 0 ? 1m : ExchangeRateToBase), 2);
    public string DisplayTotalCost => $"{CurrencyCode} {TotalCost:N2}";
    public string DisplayOutstanding => $"{CurrencyCode} {Outstanding:N2}";
}
