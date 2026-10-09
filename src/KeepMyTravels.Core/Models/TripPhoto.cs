namespace KeepMyTravels.Core.Models;

/// <summary>
/// A personal photo kept with a specific trip. The file itself lives in the app's
/// private storage; EmbeddedContentBase64 is used only while creating/importing a
/// protected .voyage transfer package.
/// </summary>
public sealed class TripPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public DateTime? CapturedDate { get; set; }
    public DateTime TakenUtc { get; set; }
    public DateTime AddedUtc { get; set; } = DateTime.UtcNow;
    public int SortOrder { get; set; }

    // Transfer-only payload. Not persisted to SQLite.
    public string? EmbeddedContentBase64 { get; set; }
}
