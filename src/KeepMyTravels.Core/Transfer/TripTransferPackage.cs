using KeepMyTravels.Core.Models;

namespace KeepMyTravels.Core.Transfer;

public sealed class TripTransferPackage
{
    public const string CurrentPackageType = "KeepMyTravels.Trip";
    public const int CurrentSchemaVersion = 5;

    public string PackageType { get; set; } = CurrentPackageType;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string SourceAppVersion { get; set; } = string.Empty;
    public DateTime ExportedUtc { get; set; } = DateTime.UtcNow;
    public Trip Trip { get; set; } = new();
    public List<ItineraryItem> ItineraryItems { get; set; } = [];
    public List<Booking> Bookings { get; set; } = [];
    public List<Expense> Expenses { get; set; } = [];
    public List<ChecklistItem> ChecklistItems { get; set; } = [];
    public List<PreTravelItem> PreTravelItems { get; set; } = [];
    public List<TravelDocument> TravelDocuments { get; set; } = [];
    public List<TravelWalletItem> TravelWalletItems { get; set; } = [];
    public List<TripPhoto> TripPhotos { get; set; } = [];
    public List<CollaborationAsset> CollaborationAssets { get; set; } = [];
    public List<TripTransferVideo> TripVideos { get; set; } = [];
    [System.Text.Json.Serialization.JsonIgnore]
    public string? TransferArchivePath { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool LegacyTransferFormat { get; set; }

    public int TotalItemCount =>
        ItineraryItems.Count + Bookings.Count + Expenses.Count + ChecklistItems.Count + PreTravelItems.Count + TravelDocuments.Count + TravelWalletItems.Count + TripPhotos.Count + TripVideos.Count;
}
