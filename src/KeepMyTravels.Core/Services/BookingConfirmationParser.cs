using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using KeepMyTravels.Core.Enums;

namespace KeepMyTravels.Core.Services;

public sealed record BookingConfirmationDraft(
    BookingType Type,
    string Name,
    string Provider,
    string Reference,
    string Address,
    DateTime? Start,
    DateTime? End,
    decimal? TotalCost,
    string CurrencyCode,
    string Notes,
    string Phone = "",
    string Website = "");

public static partial class BookingConfirmationParser
{
    public static BookingConfirmationDraft Parse(string text)
    {
        text ??= string.Empty;
        var cleaned = NormaliseText(text);
        var lines = cleaned.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var provider = InferProvider(cleaned);
        var type = InferType(cleaned);
        var reference = InferReference(cleaned);
        var name = InferName(cleaned, lines, provider, type);
        if (provider is null && type == BookingType.Accommodation && name is not null)
            provider = name;
        var address = InferAddress(lines);
        var phone = InferPhone(lines);
        var (start, end) = ParseDatesAndTimes(cleaned, type);
        var (total, currency) = ParseMoney(lines);
        var notes = BuildNotes(cleaned, lines, address, provider, phone);

        return new BookingConfirmationDraft(
            type,
            name ?? "Imported booking",
            provider ?? string.Empty,
            reference ?? string.Empty,
            address ?? string.Empty,
            start,
            end,
            total,
            currency ?? "GBP",
            notes,
            phone ?? string.Empty,
            string.Empty);
    }

    private static string NormaliseText(string text)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</(?:p|div|tr|li|h[1-6])\s*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<[^>]+>", " ", RegexOptions.IgnoreCase);
        text = WebUtility.HtmlDecode(text);
        text = text.Replace('\u00A0', ' ');
        text = Regex.Replace(text, @"!\[([^\]]*)\]\([^)]*\)", "$1");
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]*\)", "$1");
        text = text.Replace(@"\|", "¦");
        text = text.Replace("**", string.Empty).Replace("__", string.Empty).Replace("~~", string.Empty).Replace("`", string.Empty);
        text = Regex.Replace(text, @"[ \t]+", " ");

        var logicalLines = new List<string>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            line = Regex.Replace(line, @"^#{1,6}\s+", string.Empty);
            line = Regex.Replace(line, @"^>\s*", string.Empty);
            if (line.Length == 0) continue;

            var tableCandidate = line.Trim('|', ' ');
            if (line.Contains('|') && Regex.IsMatch(tableCandidate, @"^[|:\-\s]+$")) continue;

            if (line.StartsWith('|')) line = line[1..];
            if (line.EndsWith('|')) line = line[..^1];

            if (line.Contains('|'))
            {
                var cells = line.Split('|')
                    .Select(x => x.Trim().Replace('¦', '|'))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();
                if (cells.Length > 0)
                {
                    logicalLines.Add(string.Join(" | ", cells));
                    continue;
                }
            }
            logicalLines.Add(line.Replace('¦', '|'));
        }

        return string.Join("\n", logicalLines);
    }

    private static BookingType InferType(string text)
    {
        var t = text.ToLowerInvariant();
        if (t.Contains("concert") || t.Contains("festival") || t.Contains("artist presale") || t.Contains("live event") || t.Contains("sports event") || t.Contains("theatre") || t.Contains("theater") || t.Contains("cinema") || t.Contains("show tickets") || (t.Contains("tickets") && (t.Contains("venue") || t.Contains("ticket details")))) return BookingType.Event;
        if (t.Contains("boarding pass") || t.Contains("flight") || t.Contains("airline")) return BookingType.Flight;
        if (t.Contains("hotel") || t.Contains("holiday inn") || t.Contains("check-in") || t.Contains("check in") || t.Contains("accommodation") ||
            (Regex.IsMatch(t, @"\barrival\s*:") && Regex.IsMatch(t, @"\bdeparture\s*:") &&
             Regex.IsMatch(t, @"\b(?:room(?:\s+type)?|nights?|reservation\s+details)\b"))) return BookingType.Accommodation;
        if (t.Contains("train") || t.Contains("rail")) return BookingType.Train;
        if (t.Contains("ferry") || t.Contains("crossing") || t.Contains("le shuttle") || t.Contains("eurotunnel")) return BookingType.Ferry;
        if (t.Contains("restaurant") || t.Contains("dinner reservation") || t.Contains("table reservation")) return BookingType.Restaurant;
        if (t.Contains("parking")) return BookingType.Parking;
        if (t.Contains("insurance")) return BookingType.Insurance;
        if (t.Contains("attraction") || t.Contains("theme park")) return BookingType.Attraction;
        return BookingType.Other;
    }

    private static string? InferProvider(string text)
    {
        var t = text.ToLowerInvariant();
        if (t.Contains("axs") || t.Contains("axs.co.uk") || t.Contains("axs mobile id")) return "AXS";
        if (t.Contains("booking.com")) return "Booking.com";
        if (t.Contains("ticketmaster")) return "Ticketmaster";
        if (t.Contains("viator")) return "Viator";
        if (t.Contains("ihg.com") || t.Contains("holiday inn") || t.Contains("ihg")) return "IHG";
        if (t.Contains("disneyland") || t.Contains("disney")) return "Disney";
        return null;
    }

    private static string? InferReference(string text)
    {
        foreach (var pattern in new[] { ConfirmationReferencePattern(), HashReferencePattern(), GenericReferencePattern(), BookingCodePattern() })
        {
            var match = pattern.Match(text);
            if (match.Success) return match.Groups[1].Value.Trim().TrimEnd('.', ',');
        }
        return null;
    }

    private static string? InferName(string text, string[] lines, string? provider, BookingType type)
    {
        if (string.Equals(provider, "AXS", StringComparison.OrdinalIgnoreCase))
        {
            var idx = Array.FindIndex(lines, x => x.Equals("Ticket Details", StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                for (var i = idx + 1; i < Math.Min(lines.Length, idx + 4); i++)
                {
                    var candidate = CleanCandidate(lines[i]);
                    if (candidate is not null && !LooksLikeReferenceOrMetadata(candidate)) return candidate;
                }
            }
        }

        // Explicit labels such as "Hotel: Sea View Hotel" are more precise than
        // using the complete source line as an accommodation name.
        var labelled = FirstMatch(text, NamePattern());
        if (!string.IsNullOrWhiteSpace(labelled)) return CleanNameCandidate(labelled);

        if (type == BookingType.Accommodation)
        {
            var property = lines.FirstOrDefault(x => x.StartsWith("Location:", StringComparison.OrdinalIgnoreCase));
            if (property is not null)
            {
                var propertyName = CleanNameCandidate(property["Location:".Length..]);
                if (propertyName is not null) return propertyName;
            }
            // Prefer the actual property name over headings such as “Reservation” or the confirmation number.
            var accommodationCandidate = lines
                .Select(CleanNameCandidate)
                .Where(x => x is not null)
                .Select(x => x!)
                .FirstOrDefault(x => (x.Contains("Holiday Inn", StringComparison.OrdinalIgnoreCase) || x.Contains("Hotel", StringComparison.OrdinalIgnoreCase)) && !LooksLikeReferenceOrMetadata(x));
            if (accommodationCandidate is not null) return accommodationCandidate;
        }

        foreach (var line in lines)
        {
            var candidate = CleanNameCandidate(line);
            if (candidate is null) continue;
            if (!string.IsNullOrWhiteSpace(provider) && candidate.Equals(provider, StringComparison.OrdinalIgnoreCase)) continue;
            if (LooksLikeReferenceOrMetadata(candidate)) continue;
            if (candidate.Contains(" - ", StringComparison.Ordinal) && !candidate.StartsWith("Delivery Method", StringComparison.OrdinalIgnoreCase)) return candidate;
        }
        return null;
    }

    private static string? CleanCandidate(string value)
    {
        value = value.Trim().Trim('|');
        if (value.Length < 3 || value.Length > 160) return null;
        return value;
    }

    private static string? CleanNameCandidate(string value)
    {
        var candidate = CleanCandidate(value);
        if (candidate is null) return null;

        candidate = Regex.Replace(candidate, @"^(?:hotel|accommodation|property|restaurant|event|show|concert)\s*:\s*", string.Empty, RegexOptions.IgnoreCase).Trim();
        candidate = Regex.Replace(candidate, @"\s{2,}", " ").Trim();

        return candidate.Length is < 3 or > 160 ? null : candidate;
    }

    private static bool LooksLikeReferenceOrMetadata(string line) =>
        line.StartsWith("Hi ", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Thanks", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Full details", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Please note", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Item", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("calendar", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("venue", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("tickets", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("subtotal", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("delivery method", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Address:", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Front Desk:", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Email:", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Dates", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Check-in", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Check-out", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Room details", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Rate", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Summary of charges", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Total Charges", StringComparison.OrdinalIgnoreCase) ||
        line.Contains("confirmation number", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Confirmation #", StringComparison.OrdinalIgnoreCase) ||
        line.Equals("Your reservation is confirmed!", StringComparison.OrdinalIgnoreCase);

    private static string? InferAddress(string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("venue", StringComparison.OrdinalIgnoreCase) && lines[i].Contains('|'))
            {
                var split = lines[i].Split('|', 2, StringSplitOptions.TrimEntries);
                if (split.Length == 2 && split[1].Length > 3) return split[1];
            }
            if (lines[i].StartsWith("venue ", StringComparison.OrdinalIgnoreCase)) return lines[i]["venue ".Length..].Trim();
            if (lines[i].Equals("Address:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = new List<string>();
                for (var j = i + 1; j < lines.Length && parts.Count < 4; j++)
                {
                    var next = lines[j];
                    if (next.EndsWith(':') || next.StartsWith("Front Desk", StringComparison.OrdinalIgnoreCase) || next.StartsWith("Email", StringComparison.OrdinalIgnoreCase) || next.StartsWith("Dates", StringComparison.OrdinalIgnoreCase)) break;
                    var part = CleanAddressPart(next);
                    if (!string.IsNullOrWhiteSpace(part)) parts.Add(part);
                }
                var result = NormaliseAddress(string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x))));
                if (result.Length > 3) return result;
            }
        }
        return null;
    }

    private static string CleanAddressPart(string value)
    {
        return value.Trim().Trim().Trim(',', ';').Trim();
    }

    private static string NormaliseAddress(string value)
    {
        value = Regex.Replace(value, @"\s+", " ").Trim();
        value = Regex.Replace(value, @"\s*,\s*,+\s*", ", ");
        value = Regex.Replace(value, @"\s+,", ",");
        return value.Trim().Trim(',', ';').Trim();
    }

    private static string? InferPhone(string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("Front Desk", StringComparison.OrdinalIgnoreCase)) continue;
            for (var j = i + 1; j < Math.Min(lines.Length, i + 4); j++)
            {
                var digits = new string(lines[j].Where(char.IsDigit).ToArray());
                if (digits.Length >= 7) return lines[j].Trim();
            }
        }
        var contact = lines.Select(x => Regex.Match(x, @"\b(?:Tel\.?|Telephone|Phone)\s*[:.]?\s*(?<number>\+?\d[\d\s().-]{6,})", RegexOptions.IgnoreCase))
            .FirstOrDefault(x => x.Success);
        return contact?.Groups["number"].Value.Trim();
    }

    private static string BuildNotes(string text, string[] lines, string? address, string? provider, string? phone)
    {
        var notes = new List<string> { "Imported from a booking confirmation; please verify the details before saving." };
        if (!string.IsNullOrWhiteSpace(address)) notes.Add($"Address/Venue: {address}.");
        if (!string.IsNullOrWhiteSpace(phone)) notes.Add($"Phone: {phone}.");
        if (!string.IsNullOrWhiteSpace(provider) && provider.Equals("AXS", StringComparison.OrdinalIgnoreCase)) notes.Add("Delivery: AXS Mobile ID.");
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith("Email:", StringComparison.OrdinalIgnoreCase))
            {
                var inlineEmail = line["Email:".Length..].Trim();
                if (!string.IsNullOrWhiteSpace(inlineEmail))
                {
                    notes.Add($"Email: {inlineEmail}");
                }
                else if (i + 1 < lines.Length && lines[i + 1].Contains('@'))
                {
                    notes.Add($"Email: {lines[i + 1].Trim()}");
                }
                else
                {
                    notes.Add(line);
                }
            }

            if (line.StartsWith("Room details", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Rate", StringComparison.OrdinalIgnoreCase) || line.StartsWith("1 Room", StringComparison.OrdinalIgnoreCase) || line.StartsWith("1 night stay", StringComparison.OrdinalIgnoreCase)) notes.Add(line);
        }
        return string.Join(" ", notes);
    }

    private static (decimal? Amount, string? Currency) ParseMoney(string[] lines)
    {
        var candidates = new List<(int Score, int Distance, decimal Amount, string Currency)>();
        for (var i = 0; i < lines.Length; i++)
        {
            var headingScore = MoneyHeadingScore(lines[i]);
            var isMoneyHeading = headingScore > 0;
            var containsCurrency = ContainsExplicitCurrency(lines[i]);
            if (!isMoneyHeading && !containsCurrency) continue;

            var maxDistance = isMoneyHeading ? 4 : 0;
            for (var distance = 0; distance <= maxDistance && i + distance < lines.Length; distance++)
            {
                var line = lines[i + distance];
                foreach (Match m in MoneyTokenPattern().Matches(line))
                {
                    var prefix = m.Groups["prefix"].Value;
                    var suffix = m.Groups["suffix"].Value;
                    var currency = NormaliseCurrency(!string.IsNullOrWhiteSpace(prefix) ? prefix : suffix);
                    if (currency is null) continue;
                    if (!decimal.TryParse(m.Groups["amount"].Value.Replace(",", "", StringComparison.Ordinal), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)) continue;
                    // Reference-like integer strings should never become a booking amount.
                    if (amount >= 10000000m && m.Groups["amount"].Value.All(char.IsDigit)) continue;
                    var score = (headingScore > 0 ? headingScore : 60) - distance * 10;
                    candidates.Add((score, distance, amount, currency));
                }
            }
        }
        if (candidates.Count == 0) return (null, null);
        var best = candidates.OrderByDescending(x => x.Score).ThenBy(x => x.Distance).ThenBy(x => x.Amount).First();
        return (best.Amount, best.Currency);
    }

    private static bool ContainsExplicitCurrency(string line) => Regex.IsMatch(line, @"(?:GBP|EUR|USD|CAD|AUD|CHF|JPY|DKK|SEK|NOK|PLN|CZK|AED|TRY|[£€$])", RegexOptions.IgnoreCase);
    private static int MoneyHeadingScore(string line)
    {
        if (Regex.IsMatch(line, @"\btotal\s+charges\b", RegexOptions.IgnoreCase)) return 250;
        if (Regex.IsMatch(line, @"\btotal\b", RegexOptions.IgnoreCase)) return 230;
        if (Regex.IsMatch(line, @"\bsubtotal\b", RegexOptions.IgnoreCase)) return 220;
        if (Regex.IsMatch(line, @"\b(?:amount|paid)\b", RegexOptions.IgnoreCase)) return 200;
        if (Regex.IsMatch(line, @"\bsummary\s+of\s+charges\b", RegexOptions.IgnoreCase)) return 180;
        return 0;
    }

    private static string? NormaliseCurrency(string value) => value.Trim().ToUpperInvariant() switch
    {
        "£" => "GBP", "€" => "EUR", "$" => "USD", "GBP" => "GBP", "EUR" => "EUR", "USD" => "USD",
        "CAD" => "CAD", "AUD" => "AUD", "CHF" => "CHF", "JPY" => "JPY", "DKK" => "DKK", "SEK" => "SEK",
        "NOK" => "NOK", "PLN" => "PLN", "CZK" => "CZK", "AED" => "AED", "TRY" => "TRY", _ => null
    };

    private static (DateTime? Start, DateTime? End) ParseDatesAndTimes(string text, BookingType type)
    {
        var dates = new List<DateTime>();
        foreach (Match m in EventDatePattern().Matches(text))
        {
            if (TryParseDateTime(m.Groups[1].Value, m.Groups[2].Value, out var dt)) dates.Add(dt);
        }
        if (type == BookingType.Accommodation)
        {
            var arrival = FindLabelledDate(text, "Arrival", "Check-in", "Check in");
            var departure = FindLabelledDate(text, "Departure", "Check-out", "Check out");
            if (arrival.HasValue && departure.HasValue && departure.Value >= arrival.Value)
                return (arrival, departure);
            var range = AccommodationRangePattern().Match(text);
            if (range.Success && TryParseDateOnly(range.Groups[1].Value, out var startDate) && TryParseDateOnly(range.Groups[2].Value, out var endDate))
            {
                var start = ApplyTime(startDate, FindTime(text, "check-in"));
                var end = ApplyTime(endDate, FindTime(text, "check-out"));
                return (start, end);
            }
        }
        if (dates.Count == 0)
        {
            foreach (Match m in DateOnlyPattern().Matches(text).Cast<Match>().OrderBy(x => x.Index))
            {
                if (TryParseDateOnly(m.Groups[1].Value, out var d)) dates.Add(d.Date);
            }
        }
        dates = dates.Distinct().OrderBy(x => x).ToList();
        return dates.Count == 0 ? (null, null) : (dates[0], dates.Count > 1 ? dates[1] : null);
    }

    private static DateTime? FindLabelledDate(string text, params string[] labels)
    {
        foreach (var line in text.Split('\n'))
        {
            if (!labels.Any(label => line.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase))) continue;
            var match = DateOnlyPattern().Match(line);
            if (match.Success && TryParseDateOnly(match.Groups[1].Value, out var date)) return date.Date;
        }
        return null;
    }

    private static DateTime ApplyTime(DateTime date, TimeSpan? time) => time.HasValue ? date.Date.Add(time.Value) : date.Date;
    private static TimeSpan? FindTime(string text, string label)
    {
        var pattern = new Regex($@"{Regex.Escape(label)}\s*(?:[^\n]*?)?(\d{{1,2}}:\d{{2}}|\d{{1,2}}\s*:\s*\d{{2}})\s*(am|pm)?", RegexOptions.IgnoreCase);
        var match = pattern.Match(text);
        if (!match.Success) return null;
        var raw = $"{match.Groups[1].Value} {match.Groups[2].Value}".Trim();
        if (DateTime.TryParse(raw, CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.AllowWhiteSpaces, out var dt)) return dt.TimeOfDay;
        return TimeSpan.TryParse(match.Groups[1].Value.Replace(" ", "", StringComparison.Ordinal), CultureInfo.InvariantCulture, out var span) ? span : null;
    }
    private static bool TryParseDateTime(string dateText, string timeText, out DateTime value) => DateTime.TryParse(string.IsNullOrWhiteSpace(timeText) ? dateText : $"{dateText} {timeText}", CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.AllowWhiteSpaces, out value);
    private static bool TryParseDateOnly(string value, out DateTime date) => DateTime.TryParse(value, CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.AllowWhiteSpaces, out date);
    private static string? FirstMatch(string text, Regex pattern) => pattern.Match(text) is { Success: true } m ? m.Groups[1].Value.Trim() : null;

    [GeneratedRegex(@"\b(?:confirmation|reservation)\s+(?:number|no\.?|reference|ref\.?)[ \t]*(?:is\s+)?[:#]?[ \t]*([A-Z0-9-]{5,})\b", RegexOptions.IgnoreCase)] private static partial Regex ConfirmationReferencePattern();
    [GeneratedRegex(@"\b(?:confirmation|reservation)\s*#[ \t]*([A-Z0-9-]{5,})\b", RegexOptions.IgnoreCase)] private static partial Regex HashReferencePattern();
    [GeneratedRegex(@"\b(?:booking|itinerary)\s*(?:number|no\.?|reference|ref\.?)[ \t]*(?:is\s+)?[:#]?[ \t]*([A-Z0-9-]{5,})\b", RegexOptions.IgnoreCase)] private static partial Regex GenericReferencePattern();
    [GeneratedRegex(@"\bbooking\s+code\s*:\s*([A-Z0-9-]{5,})\b", RegexOptions.IgnoreCase)] private static partial Regex BookingCodePattern();
    [GeneratedRegex(@"(?:hotel|flight|train|ferry|restaurant|attraction|event|show|concert)\s*:\s*([^\r\n|]+)", RegexOptions.IgnoreCase)] private static partial Regex NamePattern();
    [GeneratedRegex(@"(?:(?<prefix>GBP|EUR|USD|CAD|AUD|CHF|JPY|DKK|SEK|NOK|PLN|CZK|AED|TRY|[£€$])\s*)?(?<amount>[0-9][0-9,.]*)\s*(?<suffix>GBP|EUR|USD|CAD|AUD|CHF|JPY|DKK|SEK|NOK|PLN|CZK|AED|TRY|[£€$])?(?=$|[^\p{L}\p{N}])", RegexOptions.IgnoreCase)] private static partial Regex MoneyTokenPattern();
    [GeneratedRegex(@"(?:calendar|date|departure|arrival|check-in|check in)\s+(?:[^\r\n|]*?\|\s*)?(?:(?:Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday),?\s+)?((?:[0-3]?\d)\s+(?:January|February|March|April|May|June|July|August|September|October|November|December|Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+20\d{2})\s*(?:[-–]\s*)?(\d{1,2}:\d{2})?", RegexOptions.IgnoreCase)] private static partial Regex EventDatePattern();
    [GeneratedRegex(@"((?:[0-3]?\d\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+20\d{2}))\s*[-–]\s*((?:[0-3]?\d\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+20\d{2}))", RegexOptions.IgnoreCase)] private static partial Regex AccommodationRangePattern();
    [GeneratedRegex(@"\b((?:[0-3]?\d[/-](?:0?[1-9]|1[0-2])[/-](?:20)?\d{2})|(?:[0-3]?\d\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+20\d{2})|(?:(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+[0-3]?\d,?\s+20\d{2}))\b", RegexOptions.IgnoreCase)] private static partial Regex DateOnlyPattern();
}
