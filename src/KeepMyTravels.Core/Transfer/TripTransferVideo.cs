namespace KeepMyTravels.Core.Transfer;

public sealed class TripTransferVideo
{
    // Device-only path in the local video index; cleared before cloud metadata/exports.
    public string? LocalFilePath { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime? CapturedDate { get; set; }
    public DateTime AddedUtc { get; set; } = DateTime.UtcNow;
}
