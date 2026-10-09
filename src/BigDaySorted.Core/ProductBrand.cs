namespace BigDaySorted.Core;

/// <summary>One implementation, selected at build time. Archive format stays shared.</summary>
public static class ProductBrand
{
#if LIVE_DAYS_SORTED
    public const string Name = "LiveDaysSorted";
    public const string TransferScheme = "livedayssorted";
    public const string Website = "https://livedayssorted.garethdherbert.chatgpt.site";
    public const string Tagline = "Every stage. Every day. Sorted.";
    public const string NewEvent = "Create a live event";
    public const string DefaultType = "Concert";
    public const string Accent = "#6841c9";
    public const string Header = "#201938";
    public const bool IsLive = true;
    public static readonly string[] Types = ["Concert", "Festival"];
#else
    public const string Name = "BigDaySorted";
    public const string TransferScheme = "bigdaysorted";
    public const string Website = "https://bigdaysorted.garethdherbert.chatgpt.site";
    public const string Tagline = "Big day? Sorted.";
    public const string NewEvent = "Create a big day";
    public const string DefaultType = "Wedding";
    public const string Accent = "#007e80";
    public const string Header = "#12323e";
    public const bool IsLive = false;
    public static readonly string[] Types = ["Wedding", "Anniversary", "Birthday", "Charity fundraiser", "Awards night", "Corporate event", "Graduation", "Community celebration"];
#endif
    public static bool IsTransferLink(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == TransferScheme && uri.Host == "transfer";
}
