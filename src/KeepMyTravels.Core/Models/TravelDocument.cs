namespace KeepMyTravels.Core.Models;

public sealed class TravelDocument
{
    public static Guid SharedDocumentsTripId => Guid.Empty;
    public bool IsShared => TripId == SharedDocumentsTripId;
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Document";
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public DateTime AddedUtc { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
    /// <summary>Embedded only in encrypted trip transfers; never persisted to SQLite.</summary>
    public string? ContentBase64 { get; set; }
}
