using KeepMyTravels.Core.Enums;

namespace KeepMyTravels.Core.Models;

public sealed class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    // Permanent provenance: a shared trip must never become a private editable copy.
    public string CollaborationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(3);
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(3).AddDays(7);
    public decimal Budget { get; set; }
    public string CurrencyCode { get; set; } = "GBP";
    public TripStatus Status { get; set; } = TripStatus.Planning;
    public string Notes { get; set; } = string.Empty;

    // Optional trip cover/background image. The file is stored in the app's private
    // data area; source/creator/license metadata is retained for stock images.
    public string BackgroundImagePath { get; set; } = string.Empty;
    public string BackgroundImageSourceUrl { get; set; } = string.Empty;
    public string BackgroundImageCreator { get; set; } = string.Empty;
    public string BackgroundImageLicense { get; set; } = string.Empty;
    public string BackgroundImageLicenseUrl { get; set; } = string.Empty;
    public string BackgroundImageProvider { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public int NumberOfDays => Math.Max(1, (EndDate.Date - StartDate.Date).Days + 1);
}
