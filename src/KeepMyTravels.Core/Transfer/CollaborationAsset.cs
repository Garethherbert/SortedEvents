namespace KeepMyTravels.Core.Transfer;

public sealed class CollaborationAsset
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public int KeyEpoch { get; set; }
    public long Bytes { get; set; }
    public int Chunks { get; set; }
}
