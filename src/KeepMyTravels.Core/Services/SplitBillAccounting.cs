namespace KeepMyTravels.Core.Services;

public sealed record BillShareAmount(string AccountId, long AmountMinor);
public sealed record SharedBillAmount(string PaidByAccountId, string CurrencyCode, long TotalMinor, IReadOnlyList<BillShareAmount> Shares);
public sealed record SettledPayment(string FromAccountId, string ToAccountId, string CurrencyCode, long AmountMinor);
public sealed record ParticipantBalance(string AccountId, string CurrencyCode, long AmountMinor);

public static class SplitBillAccounting
{
    // Positive means the participant should receive money. Currencies are never mixed.
    public static IReadOnlyList<ParticipantBalance> Calculate(IEnumerable<SharedBillAmount> bills, IEnumerable<SettledPayment> confirmedPayments)
    {
        var balances = new Dictionary<(string Account, string Currency), long>();
        void Add(string account, string currency, long value)
        { var key = (account, currency); balances[key] = checked(balances.GetValueOrDefault(key) + value); }
        foreach (var bill in bills)
        {
            if (bill.TotalMinor <= 0 || bill.Shares.Any(x => x.AmountMinor < 0) || bill.Shares.Select(x => x.AccountId).Distinct().Count() != bill.Shares.Count)
                throw new ArgumentException("Invalid bill shares.");
            if (bill.Shares.Sum(x => x.AmountMinor) != bill.TotalMinor) continue;
            Add(bill.PaidByAccountId, bill.CurrencyCode, bill.TotalMinor);
            foreach (var share in bill.Shares) Add(share.AccountId, bill.CurrencyCode, -share.AmountMinor);
        }
        foreach (var payment in confirmedPayments)
        {
            if (payment.AmountMinor <= 0 || payment.FromAccountId == payment.ToAccountId) throw new ArgumentException("Invalid settlement.");
            Add(payment.FromAccountId, payment.CurrencyCode, payment.AmountMinor);
            Add(payment.ToAccountId, payment.CurrencyCode, -payment.AmountMinor);
        }
        return balances.OrderBy(x => x.Key.Currency).ThenBy(x => x.Key.Account)
            .Select(x => new ParticipantBalance(x.Key.Account, x.Key.Currency, x.Value)).ToList();
    }
}
