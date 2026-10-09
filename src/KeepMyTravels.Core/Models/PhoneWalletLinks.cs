namespace KeepMyTravels.Core.Models;

public static class PhoneWalletLinks
{
    public static bool TryGetSaveUri(string? value, string provider, out Uri? uri)
    {
        uri = null;
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var candidate)
            || candidate.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(candidate.UserInfo)
            || !candidate.IsDefaultPort) return false;
        var google = candidate.Host.Equals("pay.google.com", StringComparison.OrdinalIgnoreCase)
            && candidate.AbsolutePath.StartsWith("/gp/v/save/", StringComparison.Ordinal)
            && candidate.AbsolutePath.Length > "/gp/v/save/".Length;
        var samsung = (candidate.Host.Equals("a.swallet.link", StringComparison.OrdinalIgnoreCase)
                && candidate.AbsolutePath.StartsWith("/atw/", StringComparison.Ordinal))
            || (candidate.Host.EndsWith(".walletsvc.samsung.com", StringComparison.OrdinalIgnoreCase)
                && candidate.AbsolutePath.Length > 1);
        if ((provider == "google" && google) || (provider == "samsung" && samsung))
        {
            uri = candidate;
            return true;
        }
        return false;
    }
}
