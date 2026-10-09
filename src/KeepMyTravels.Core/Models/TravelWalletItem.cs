using KeepMyTravels.Core.Enums;

namespace KeepMyTravels.Core.Models;

public sealed class TravelWalletItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public TravelWalletCategory Category { get; set; } = TravelWalletCategory.Other;
    public string Title { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string QrPayload { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public string GoogleWalletUrl { get; set; } = string.Empty;
    public string SamsungWalletUrl { get; set; } = string.Empty;
}
