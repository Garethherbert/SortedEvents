namespace KeepMyTravels.Core.Abstractions;

/// <summary>Controls which local database is active for the signed-in KeepMyTravels account.</summary>
public interface IAccountDataScope
{
    string? CurrentAccountId { get; }
    string CurrentDatabasePath { get; }

    Task SwitchAccountAsync(string? accountId);
    Task<bool> HasLegacyLocalDataAsync();
    Task<bool> ClaimLegacyLocalDataAsync();
}
