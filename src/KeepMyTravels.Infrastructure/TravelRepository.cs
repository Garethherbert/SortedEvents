using SQLite;
using KeepMyTravels.Core.Abstractions;
using KeepMyTravels.Core.Enums;
using KeepMyTravels.Core.Models;
using KeepMyTravels.Core.Transfer;
using KeepMyTravels.Infrastructure.Entities;

namespace KeepMyTravels.Infrastructure;

public sealed class TravelRepository : ITravelRepository, ITripPackageRepository, IAccountDataScope
{
    private readonly string _legacyDatabasePath;
    private readonly string _accountsDirectory;
    private readonly string _legacyClaimMarkerPath;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private SQLiteAsyncConnection _database;
    private string _databasePath;
    private string? _currentAccountId;
    private bool _initialized;

    public TravelRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _legacyDatabasePath = Path.GetFullPath(databasePath);
        var root = Path.GetDirectoryName(_legacyDatabasePath)
            ?? throw new InvalidOperationException("The local database path has no parent directory.");
        _accountsDirectory = Path.Combine(root, "Accounts");
        _legacyClaimMarkerPath = _legacyDatabasePath + ".claimed";
        _databasePath = _legacyDatabasePath;
        _database = CreateConnection(_databasePath);
    }

    public string? CurrentAccountId => _currentAccountId;
    public string CurrentDatabasePath => _databasePath;

    public async Task SwitchAccountAsync(string? accountId)
    {
        accountId = string.IsNullOrWhiteSpace(accountId) ? null : accountId.Trim();
        if (string.Equals(_currentAccountId, accountId, StringComparison.Ordinal) && _initialized)
            return;

        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (string.Equals(_currentAccountId, accountId, StringComparison.Ordinal) && _initialized)
                return;

            await _database.CloseAsync().ConfigureAwait(false);
            _initialized = false;

            _currentAccountId = accountId;
            _databasePath = GetDatabasePath(accountId);

            if (accountId is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
            }

            _database = CreateConnection(_databasePath);
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task<bool> HasLegacyLocalDataAsync()
    {
        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_currentAccountId is null || !File.Exists(_legacyDatabasePath) || File.Exists(_legacyClaimMarkerPath))
                return false;

            var targetPath = GetDatabasePath(_currentAccountId);
            if (File.Exists(targetPath))
            {
                var target = CreateConnection(targetPath);
                try
                {
                    var targetTables = await target.QueryAsync<SqliteTableInfo>(
                        "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';").ConfigureAwait(false);
                    if (targetTables.Count > 0)
                        return false;
                }
                finally
                {
                    await target.CloseAsync().ConfigureAwait(false);
                }
            }

            var legacy = CreateConnection(_legacyDatabasePath);
            try
            {
                var tables = await legacy.QueryAsync<SqliteTableInfo>(
                    "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';").ConfigureAwait(false);
                var existing = tables.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
                if (!existing.Contains(DatabaseSchema.A1))
                    return false;
                var count = await legacy.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {DatabaseSchema.Quote(DatabaseSchema.A1)};").ConfigureAwait(false);
                return count > 0;
            }
            finally
            {
                await legacy.CloseAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task<bool> ClaimLegacyLocalDataAsync()
    {
        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_currentAccountId is null || !File.Exists(_legacyDatabasePath) || File.Exists(_legacyClaimMarkerPath))
                return false;

            var targetPath = GetDatabasePath(_currentAccountId);
            await _database.CloseAsync().ConfigureAwait(false);
            _initialized = false;

            if (File.Exists(targetPath))
            {
                var target = CreateConnection(targetPath);
                try
                {
                    var tables = await target.QueryAsync<SqliteTableInfo>(
                        "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';").ConfigureAwait(false);
                    if (tables.Count > 0)
                    {
                        _database = CreateConnection(targetPath);
                        return false;
                    }
                }
                finally
                {
                    await target.CloseAsync().ConfigureAwait(false);
                }
                File.Delete(targetPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            var legacy = CreateConnection(_legacyDatabasePath);
            try
            {
                var tables = await legacy.QueryAsync<SqliteTableInfo>(
                    "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';").ConfigureAwait(false);
                var existing = tables.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
                if (!existing.Contains(DatabaseSchema.A1))
                {
                    _database = CreateConnection(targetPath);
                    return false;
                }

                var count = await legacy.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {DatabaseSchema.Quote(DatabaseSchema.A1)};").ConfigureAwait(false);
                if (count <= 0)
                {
                    _database = CreateConnection(targetPath);
                    return false;
                }

                await legacy.BackupAsync(targetPath).ConfigureAwait(false);
                await File.WriteAllTextAsync(_legacyClaimMarkerPath, _currentAccountId).ConfigureAwait(false);
            }
            finally
            {
                await legacy.CloseAsync().ConfigureAwait(false);
            }

            // Move the old shared database out of the active path so a different account
            // can never inherit it. It is retained for recovery rather than deleted.
            var archiveDirectory = Path.Combine(Path.GetDirectoryName(_legacyDatabasePath)!, "LegacyMigrated");
            Directory.CreateDirectory(archiveDirectory);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var archivePath = Path.Combine(archiveDirectory, $"keepmytravels_{stamp}_{_currentAccountId}.db3");
            File.Move(_legacyDatabasePath, archivePath, overwrite: false);
            TryMoveSidecar(_legacyDatabasePath + "-wal", archivePath + "-wal");
            TryMoveSidecar(_legacyDatabasePath + "-shm", archivePath + "-shm");

            _databasePath = targetPath;
            _database = CreateConnection(_databasePath);
            _initialized = false;
            await InitializeDatabaseCoreAsync().ConfigureAwait(false);
            _initialized = true;
            return true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private string GetDatabasePath(string? accountId)
    {
        if (accountId is null)
            return _legacyDatabasePath;

        var safeId = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(accountId))).ToLowerInvariant();
        return Path.Combine(_accountsDirectory, safeId, "keepmytravels.db3");
    }

    private static SQLiteAsyncConnection CreateConnection(string databasePath) =>
        new(databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);

    private static void TryMoveSidecar(string source, string destination)
    {
        if (!File.Exists(source))
            return;
        try { File.Move(source, destination, overwrite: false); } catch { }
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_initialized)
                return;
            await InitializeDatabaseCoreAsync().ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task InitializeDatabaseCoreAsync()
    {
        await MigrateLegacySchemaAsync().ConfigureAwait(false);
        await _database.CreateTableAsync<TripEntity>().ConfigureAwait(false);
        await EnsureTripBackgroundColumnsAsync().ConfigureAwait(false);
        await _database.CreateTableAsync<ItineraryItemEntity>().ConfigureAwait(false);
        await _database.CreateTableAsync<BookingEntity>().ConfigureAwait(false);
        await _database.CreateTableAsync<ExpenseEntity>().ConfigureAwait(false);
        await _database.CreateTableAsync<ChecklistItemEntity>().ConfigureAwait(false);
        await _database.CreateTableAsync<PreTravelItemEntity>().ConfigureAwait(false);
        await _database.CreateTableAsync<TravelDocumentEntity>().ConfigureAwait(false);
        await _database.CreateTableAsync<TravelWalletItemEntity>().ConfigureAwait(false);
        await _database.CreateTableAsync<TripPhotoEntity>().ConfigureAwait(false);
        await _database.ExecuteAsync("CREATE TABLE IF NOT EXISTS k_sync_clock(id INTEGER PRIMARY KEY,revision INTEGER NOT NULL)").ConfigureAwait(false);
        await _database.ExecuteAsync("INSERT OR IGNORE INTO k_sync_clock VALUES(1,0)").ConfigureAwait(false);
        await _database.ExecuteAsync("CREATE TABLE IF NOT EXISTS k_sync_state(TripId TEXT PRIMARY KEY,Revision INTEGER NOT NULL,Hash TEXT NOT NULL)").ConfigureAwait(false);
        await _database.ExecuteAsync("CREATE TABLE IF NOT EXISTS k_sync_deletes(TripId TEXT PRIMARY KEY,Revision INTEGER NOT NULL)").ConfigureAwait(false);
        foreach (var table in new[] {DatabaseSchema.A1,DatabaseSchema.A2,DatabaseSchema.A3,DatabaseSchema.A4,DatabaseSchema.A5,DatabaseSchema.A6,DatabaseSchema.A7,DatabaseSchema.A8,DatabaseSchema.A9})
            foreach (var operation in new[] {"INSERT","UPDATE","DELETE"})
                await _database.ExecuteAsync($"CREATE TRIGGER IF NOT EXISTS {DatabaseSchema.Quote("sync_" + table + "_" + operation)} AFTER {operation} ON {DatabaseSchema.Quote(table)} BEGIN UPDATE k_sync_clock SET revision=revision+1 WHERE id=1; END").ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Trip>> GetTripsAsync()
    {
        await InitializeAsync().ConfigureAwait(false);
        var rows = await _database.Table<TripEntity>()
            .ToListAsync()
            .ConfigureAwait(false);
        return rows.OrderBy(x => x.StartDate).Select(ToModel).ToList();
    }

    public async Task<Trip?> GetTripAsync(Guid tripId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var row = await _database.FindAsync<TripEntity>(tripId.ToString("D")).ConfigureAwait(false);
        return row is null ? null : ToModel(row);
    }

    public async Task UpsertTripAsync(Trip trip)
    {
        await InitializeAsync().ConfigureAwait(false);
        trip.UpdatedUtc = DateTime.UtcNow;
        if (trip.CreatedUtc == default)
        {
            trip.CreatedUtc = trip.UpdatedUtc;
        }
        await _database.InsertOrReplaceAsync(ToEntity(trip)).ConfigureAwait(false);
    }

    public async Task UpsertTripFromSyncAsync(Trip trip)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.InsertOrReplaceAsync(ToEntity(trip)).ConfigureAwait(false);
    }

    public async Task DeleteTripAsync(Guid tripId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var id = tripId.ToString("D");
        await _database.RunInTransactionAsync(connection =>
        {
            connection.Execute("INSERT OR REPLACE INTO k_sync_deletes(TripId,Revision) VALUES(?,COALESCE((SELECT Revision FROM k_sync_state WHERE TripId=?),0))",id,id);
            connection.Execute($"DELETE FROM {DatabaseSchema.A2} WHERE {DatabaseSchema.B02} = ?", id);
            connection.Execute($"DELETE FROM {DatabaseSchema.A3} WHERE {DatabaseSchema.B02} = ?", id);
            connection.Execute($"DELETE FROM {DatabaseSchema.A4} WHERE {DatabaseSchema.B02} = ?", id);
            connection.Execute($"DELETE FROM {DatabaseSchema.A5} WHERE {DatabaseSchema.B02} = ?", id);
            connection.Execute($"DELETE FROM {DatabaseSchema.A9} WHERE {DatabaseSchema.B02} = ?", id);
            connection.Execute($"DELETE FROM {DatabaseSchema.A7} WHERE {DatabaseSchema.B02} = ?", id);
            connection.Execute($"DELETE FROM {DatabaseSchema.A6} WHERE {DatabaseSchema.B02} = ?", id);
            connection.Execute($"DELETE FROM {DatabaseSchema.A8} WHERE {DatabaseSchema.B02} = ?", id);
            connection.Execute($"DELETE FROM {DatabaseSchema.A1} WHERE {DatabaseSchema.B01} = ?", id);
        }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ItineraryItem>> GetItineraryAsync(Guid tripId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var id = tripId.ToString("D");
        var rows = await _database.Table<ItineraryItemEntity>()
            .Where(x => x.TripId == id)
            .ToListAsync()
            .ConfigureAwait(false);
        return rows.OrderBy(x => x.DayNumber).ThenBy(x => x.SortOrder).Select(ToModel).ToList();
    }

    public async Task<ItineraryItem?> GetItineraryItemAsync(Guid itemId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var row = await _database.FindAsync<ItineraryItemEntity>(itemId.ToString("D")).ConfigureAwait(false);
        return row is null ? null : ToModel(row);
    }

    public async Task UpsertItineraryItemAsync(ItineraryItem item)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.InsertOrReplaceAsync(ToEntity(item)).ConfigureAwait(false);
    }

    public async Task DeleteItineraryItemAsync(Guid itemId)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.DeleteAsync<ItineraryItemEntity>(itemId.ToString("D")).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid tripId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var id = tripId.ToString("D");
        var rows = await _database.Table<BookingEntity>()
            .Where(x => x.TripId == id)
            .ToListAsync()
            .ConfigureAwait(false);
        return rows.OrderBy(x => x.Type).ThenBy(x => x.Name).Select(ToModel).ToList();
    }

    public async Task<Booking?> GetBookingAsync(Guid bookingId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var row = await _database.FindAsync<BookingEntity>(bookingId.ToString("D")).ConfigureAwait(false);
        return row is null ? null : ToModel(row);
    }

    public async Task UpsertBookingAsync(Booking booking)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.InsertOrReplaceAsync(ToEntity(booking)).ConfigureAwait(false);
    }

    public async Task DeleteBookingAsync(Guid bookingId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var id = bookingId.ToString("D");
        await _database.RunInTransactionAsync(connection =>
        {
            connection.Execute($"UPDATE {DatabaseSchema.A2} SET {DatabaseSchema.B26} = NULL WHERE {DatabaseSchema.B26} = ?", id);
            connection.Execute($"UPDATE {DatabaseSchema.A4} SET {DatabaseSchema.B26} = NULL WHERE {DatabaseSchema.B26} = ?", id);
            connection.Execute($"DELETE FROM {DatabaseSchema.A3} WHERE {DatabaseSchema.B01} = ?", id);
        }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Expense>> GetExpensesAsync(Guid tripId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var id = tripId.ToString("D");
        var rows = await _database.Table<ExpenseEntity>()
            .Where(x => x.TripId == id)
            .ToListAsync()
            .ConfigureAwait(false);
        return rows.OrderByDescending(x => x.Date).Select(ToModel).ToList();
    }

    public async Task<Expense?> GetExpenseAsync(Guid expenseId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var row = await _database.FindAsync<ExpenseEntity>(expenseId.ToString("D")).ConfigureAwait(false);
        return row is null ? null : ToModel(row);
    }

    public async Task UpsertExpenseAsync(Expense expense)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.InsertOrReplaceAsync(ToEntity(expense)).ConfigureAwait(false);
    }

    public async Task DeleteExpenseAsync(Guid expenseId)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.DeleteAsync<ExpenseEntity>(expenseId.ToString("D")).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ChecklistItem>> GetChecklistItemsAsync(Guid tripId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var id = tripId.ToString("D");
        var rows = await _database.Table<ChecklistItemEntity>()
            .Where(x => x.TripId == id)
            .ToListAsync()
            .ConfigureAwait(false);
        return rows.OrderBy(x => x.ListName).ThenBy(x => x.SortOrder).Select(ToModel).ToList();
    }

    public async Task UpsertChecklistItemAsync(ChecklistItem item)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.InsertOrReplaceAsync(ToEntity(item)).ConfigureAwait(false);
    }

    public async Task DeleteChecklistItemAsync(Guid itemId)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.DeleteAsync<ChecklistItemEntity>(itemId.ToString("D")).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PreTravelItem>> GetPreTravelItemsAsync(Guid tripId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var id = tripId.ToString("D");
        var rows = await _database.Table<PreTravelItemEntity>()
            .Where(x => x.TripId == id)
            .ToListAsync()
            .ConfigureAwait(false);
        return rows.OrderBy(x => x.DueDate).ThenBy(x => x.Category).ThenBy(x => x.SortOrder).Select(ToModel).ToList();
    }

    public async Task UpsertPreTravelItemAsync(PreTravelItem item)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.InsertOrReplaceAsync(ToEntity(item)).ConfigureAwait(false);
    }

    public async Task DeletePreTravelItemAsync(Guid itemId)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.DeleteAsync<PreTravelItemEntity>(itemId.ToString("D")).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TravelDocument>> GetTravelDocumentsAsync(Guid tripId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var id = tripId.ToString("D");
        var rows = await _database.Table<TravelDocumentEntity>()
            .Where(x => x.TripId == id)
            .ToListAsync().ConfigureAwait(false);
        return rows.OrderByDescending(x => x.AddedUtc).Select(ToModel).ToList();
    }

    public async Task<TravelDocument?> GetTravelDocumentAsync(Guid documentId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var row = await _database.FindAsync<TravelDocumentEntity>(documentId.ToString("D")).ConfigureAwait(false);
        return row is null ? null : ToModel(row);
    }

    public async Task UpsertTravelDocumentAsync(TravelDocument document)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.InsertOrReplaceAsync(ToEntity(document)).ConfigureAwait(false);
    }

    public async Task DeleteTravelDocumentAsync(Guid documentId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var id = documentId.ToString("D");
        await _database.RunInTransactionAsync(connection =>
        {
            connection.Execute($"UPDATE {DatabaseSchema.A7} SET {DatabaseSchema.B51} = NULL WHERE {DatabaseSchema.B51} = ?", id);
            connection.Execute($"DELETE FROM {DatabaseSchema.A6} WHERE {DatabaseSchema.B01} = ?", id);
        }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TravelWalletItem>> GetTravelWalletItemsAsync(Guid tripId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var id = tripId.ToString("D");
        var rows = await _database.Table<TravelWalletItemEntity>()
            .Where(x => x.TripId == id)
            .ToListAsync().ConfigureAwait(false);
        return rows.OrderBy(x => x.Category).ThenBy(x => x.Title).Select(ToModel).ToList();
    }

    public async Task<TravelWalletItem?> GetTravelWalletItemAsync(Guid itemId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var row = await _database.FindAsync<TravelWalletItemEntity>(itemId.ToString("D")).ConfigureAwait(false);
        return row is null ? null : ToModel(row);
    }

    public async Task UpsertTravelWalletItemAsync(TravelWalletItem item)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.InsertOrReplaceAsync(ToEntity(item)).ConfigureAwait(false);
    }

    public async Task DeleteTravelWalletItemAsync(Guid itemId)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.DeleteAsync<TravelWalletItemEntity>(itemId.ToString("D")).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TripPhoto>> GetTripPhotosAsync(Guid tripId)
    {
        await InitializeAsync().ConfigureAwait(false);
        var id = tripId.ToString("D");
        var rows = await _database.Table<TripPhotoEntity>()
            .Where(x => x.TripId == id)
            .ToListAsync().ConfigureAwait(false);
        return rows.OrderByDescending(x => x.TakenUtc).ThenByDescending(x => x.AddedUtc).Select(ToModel).ToList();
    }

    public async Task UpsertTripPhotoAsync(TripPhoto photo)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.InsertOrReplaceAsync(ToEntity(photo)).ConfigureAwait(false);
    }

    public async Task DeleteTripPhotoAsync(Guid photoId)
    {
        await InitializeAsync().ConfigureAwait(false);
        await _database.DeleteAsync<TripPhotoEntity>(photoId.ToString("D")).ConfigureAwait(false);
    }

    public async Task<TripTransferPackage?> CreateTripPackageAsync(Guid tripId, string sourceAppVersion)
    {
        var trip = await GetTripAsync(tripId).ConfigureAwait(false);
        if (trip is null)
        {
            return null;
        }

        return new TripTransferPackage
        {
            SourceAppVersion = sourceAppVersion,
            ExportedUtc = DateTime.UtcNow,
            Trip = trip,
            ItineraryItems = (await GetItineraryAsync(tripId).ConfigureAwait(false)).ToList(),
            Bookings = (await GetBookingsAsync(tripId).ConfigureAwait(false)).ToList(),
            Expenses = (await GetExpensesAsync(tripId).ConfigureAwait(false)).ToList(),
            ChecklistItems = (await GetChecklistItemsAsync(tripId).ConfigureAwait(false)).ToList(),
            PreTravelItems = (await GetPreTravelItemsAsync(tripId).ConfigureAwait(false)).ToList(),
            TravelDocuments = (await GetTravelDocumentsAsync(tripId).ConfigureAwait(false)).ToList(),
            TravelWalletItems = (await GetTravelWalletItemsAsync(tripId).ConfigureAwait(false)).ToList(),
            TripPhotos = (await GetTripPhotosAsync(tripId).ConfigureAwait(false)).ToList()
        };
    }

    public async Task<bool> TripExistsAsync(Guid tripId)
    {
        await InitializeAsync().ConfigureAwait(false);
        return await _database.FindAsync<TripEntity>(tripId.ToString("D")).ConfigureAwait(false) is not null;
    }

    public async Task<Guid> ImportTripPackageAsync(TripTransferPackage package, TripImportMode mode)
    {
        TripTransferPackageValidator.Validate(package);
        await InitializeAsync().ConfigureAwait(false);

        if (mode == TripImportMode.CreateCopy && !string.IsNullOrEmpty(package.Trip.CollaborationId))
            throw new InvalidOperationException("A collaboration trip cannot be imported as a private copy.");
        var importPackage = mode == TripImportMode.CreateCopy
            ? CreateCopy(package)
            : ClonePackage(package);

        var tripId = importPackage.Trip.Id.ToString("D");
        await _database.RunInTransactionAsync(connection =>
        {
            if (mode == TripImportMode.PreserveIds)
            {
                DeleteTripGraph(connection, tripId);
            }

            connection.InsertOrReplace(ToEntity(importPackage.Trip));
            foreach (var booking in importPackage.Bookings)
            {
                connection.InsertOrReplace(ToEntity(booking));
            }
            foreach (var item in importPackage.ItineraryItems)
            {
                connection.InsertOrReplace(ToEntity(item));
            }
            foreach (var expense in importPackage.Expenses)
            {
                connection.InsertOrReplace(ToEntity(expense));
            }
            foreach (var checklistItem in importPackage.ChecklistItems)
            {
                connection.InsertOrReplace(ToEntity(checklistItem));
            }
            foreach (var preTravelItem in importPackage.PreTravelItems)
            {
                connection.InsertOrReplace(ToEntity(preTravelItem));
            }
            foreach (var document in importPackage.TravelDocuments)
            {
                connection.InsertOrReplace(ToEntity(document));
            }
            foreach (var walletItem in importPackage.TravelWalletItems)
            {
                connection.InsertOrReplace(ToEntity(walletItem));
            }
            foreach (var photo in importPackage.TripPhotos)
            {
                connection.InsertOrReplace(ToEntity(photo));
            }
        }).ConfigureAwait(false);

        return importPackage.Trip.Id;
    }

    public async Task<Guid> ReplaceAllWithTripPackageAsync(TripTransferPackage package)
    {
        TripTransferPackageValidator.Validate(package);
        await InitializeAsync().ConfigureAwait(false);

        var importPackage = ClonePackage(package);
        var tripId = importPackage.Trip.Id.ToString("D");

        await _database.RunInTransactionAsync(connection =>
        {
            // Recovery mode is deliberately destructive: the existing local data is known
            // to have failed integrity validation, so restore one trusted trip graph only.
            connection.Execute($"DELETE FROM {DatabaseSchema.A2}");
            connection.Execute($"DELETE FROM {DatabaseSchema.A3}");
            connection.Execute($"DELETE FROM {DatabaseSchema.A4}");
            connection.Execute($"DELETE FROM {DatabaseSchema.A5}");
            connection.Execute($"DELETE FROM {DatabaseSchema.A9}");
            connection.Execute($"DELETE FROM {DatabaseSchema.A7}");
            // Shared documents are account-level, not part of any one trip. Recovery restore
            // replaces the trip graph but must not erase passports, insurance or other
            // central documents that the paid account deliberately stored once for all trips.
            connection.Execute($"DELETE FROM {DatabaseSchema.A6} WHERE {DatabaseSchema.B02} <> ?", TravelDocument.SharedDocumentsTripId.ToString("D"));
            connection.Execute($"DELETE FROM {DatabaseSchema.A8}");
            connection.Execute($"DELETE FROM {DatabaseSchema.A1}");

            connection.InsertOrReplace(ToEntity(importPackage.Trip));
            foreach (var booking in importPackage.Bookings) connection.InsertOrReplace(ToEntity(booking));
            foreach (var item in importPackage.ItineraryItems) connection.InsertOrReplace(ToEntity(item));
            foreach (var expense in importPackage.Expenses) connection.InsertOrReplace(ToEntity(expense));
            foreach (var checklistItem in importPackage.ChecklistItems) connection.InsertOrReplace(ToEntity(checklistItem));
            foreach (var preTravelItem in importPackage.PreTravelItems) connection.InsertOrReplace(ToEntity(preTravelItem));
            foreach (var document in importPackage.TravelDocuments) connection.InsertOrReplace(ToEntity(document));
            foreach (var walletItem in importPackage.TravelWalletItems) connection.InsertOrReplace(ToEntity(walletItem));
            foreach (var photo in importPackage.TripPhotos) connection.InsertOrReplace(ToEntity(photo));
        }).ConfigureAwait(false);

        return Guid.Parse(tripId);
    }

    public sealed class PersonalSyncState { public string TripId { get; set; } = ""; public long Revision { get; set; } public string Hash { get; set; } = ""; }
    public sealed class PersonalSyncDelete { public string TripId { get; set; } = ""; public long Revision { get; set; } }
    public async Task<long> ReadSyncClockAsync() { await InitializeAsync(); return await _database.ExecuteScalarAsync<long>("SELECT revision FROM k_sync_clock WHERE id=1"); }
    public async Task<List<PersonalSyncState>> ReadSyncStatesAsync() { await InitializeAsync(); return await _database.QueryAsync<PersonalSyncState>("SELECT * FROM k_sync_state"); }
    public async Task<List<PersonalSyncDelete>> ReadSyncDeletesAsync() { await InitializeAsync(); return await _database.QueryAsync<PersonalSyncDelete>("SELECT * FROM k_sync_deletes"); }
    public async Task SaveSyncStateAsync(string accountId, Guid tripId, long revision, string hash)
    {
        await _initializationLock.WaitAsync();
        try {
            if (_currentAccountId != accountId) throw new InvalidOperationException("Account changed during sync.");
            await _database.ExecuteAsync("INSERT OR REPLACE INTO k_sync_state VALUES(?,?,?)",tripId.ToString("D"),revision,hash);
        } finally { _initializationLock.Release(); }
    }
    public async Task AcknowledgeSyncDeleteAsync(string accountId, string tripId)
    {
        await _initializationLock.WaitAsync();
        try {
            if (_currentAccountId != accountId) throw new InvalidOperationException("Account changed during sync.");
            await _database.ExecuteAsync("DELETE FROM k_sync_deletes WHERE TripId=?",tripId);
            await _database.ExecuteAsync("DELETE FROM k_sync_state WHERE TripId=?",tripId);
        } finally { _initializationLock.Release(); }
    }
    public async Task<bool> ApplyPersonalSyncAsync(string accountId, TripTransferPackage? package, Guid tripId, long expectedClock, long revision, string hash, Action? applyMedia = null)
    {
        if (package is not null) {
            TripTransferPackageValidator.Validate(package);
            if (!string.IsNullOrEmpty(package.Trip.CollaborationId) || package.Trip.Id != tripId)
                throw new InvalidDataException("Invalid personal sync trip.");
        }
        var applied=false;
        await _initializationLock.WaitAsync();
        try {
            if (_currentAccountId != accountId) throw new InvalidOperationException("Account changed during sync.");
            await _database.RunInTransactionAsync(connection => {
                if(connection.ExecuteScalar<long>("SELECT revision FROM k_sync_clock WHERE id=1")!=expectedClock) return;
                var existing=connection.Find<TripEntity>(tripId.ToString("D"));
                if(!string.IsNullOrEmpty(existing?.CollaborationId)) throw new InvalidOperationException("Sync cannot replace a shared trip.");
                if(package is not null) {
                    var owners=new (string Table,IEnumerable<Guid> Ids)[] {
                        (DatabaseSchema.A2,package.ItineraryItems.Select(x=>x.Id)),(DatabaseSchema.A3,package.Bookings.Select(x=>x.Id)),
                        (DatabaseSchema.A4,package.Expenses.Select(x=>x.Id)),(DatabaseSchema.A5,package.ChecklistItems.Select(x=>x.Id)),
                        (DatabaseSchema.A9,package.PreTravelItems.Select(x=>x.Id)),(DatabaseSchema.A6,package.TravelDocuments.Select(x=>x.Id)),
                        (DatabaseSchema.A7,package.TravelWalletItems.Select(x=>x.Id)),(DatabaseSchema.A8,package.TripPhotos.Select(x=>x.Id)) };
                    foreach(var group in owners) foreach(var itemId in group.Ids)
                        if(connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {DatabaseSchema.Quote(group.Table)} WHERE {DatabaseSchema.Quote(DatabaseSchema.B01)}=? AND {DatabaseSchema.Quote(DatabaseSchema.B02)}<>?",itemId.ToString("D"),tripId.ToString("D"))>0)
                            throw new InvalidDataException("A sync item belongs to another local trip.");
                }
                DeleteTripGraph(connection,tripId.ToString("D"));
                if(package is not null) {
                    connection.InsertOrReplace(ToEntity(package.Trip));
                    foreach(var x in package.Bookings) connection.InsertOrReplace(ToEntity(x));
                    foreach(var x in package.ItineraryItems) connection.InsertOrReplace(ToEntity(x));
                    foreach(var x in package.Expenses) connection.InsertOrReplace(ToEntity(x));
                    foreach(var x in package.ChecklistItems) connection.InsertOrReplace(ToEntity(x));
                    foreach(var x in package.PreTravelItems) connection.InsertOrReplace(ToEntity(x));
                    foreach(var x in package.TravelDocuments) connection.InsertOrReplace(ToEntity(x));
                    foreach(var x in package.TravelWalletItems) connection.InsertOrReplace(ToEntity(x));
                    foreach(var x in package.TripPhotos) connection.InsertOrReplace(ToEntity(x));
                }
                applyMedia?.Invoke();
                connection.Execute("INSERT OR REPLACE INTO k_sync_state VALUES(?,?,?)",tripId.ToString("D"),revision,hash);
                applied=true;
            });
        } finally { _initializationLock.Release(); }
        return applied;
    }

    private static void DeleteTripGraph(SQLiteConnection connection, string tripId)
    {
        connection.Execute($"DELETE FROM {DatabaseSchema.A2} WHERE {DatabaseSchema.B02} = ?", tripId);
        connection.Execute($"DELETE FROM {DatabaseSchema.A3} WHERE {DatabaseSchema.B02} = ?", tripId);
        connection.Execute($"DELETE FROM {DatabaseSchema.A4} WHERE {DatabaseSchema.B02} = ?", tripId);
        connection.Execute($"DELETE FROM {DatabaseSchema.A5} WHERE {DatabaseSchema.B02} = ?", tripId);
        connection.Execute($"DELETE FROM {DatabaseSchema.A9} WHERE {DatabaseSchema.B02} = ?", tripId);
        connection.Execute($"DELETE FROM {DatabaseSchema.A7} WHERE {DatabaseSchema.B02} = ?", tripId);
        connection.Execute($"DELETE FROM {DatabaseSchema.A6} WHERE {DatabaseSchema.B02} = ?", tripId);
        connection.Execute($"DELETE FROM {DatabaseSchema.A8} WHERE {DatabaseSchema.B02} = ?", tripId);
        connection.Execute($"DELETE FROM {DatabaseSchema.A1} WHERE {DatabaseSchema.B01} = ?", tripId);
    }

    private static TripTransferPackage CreateCopy(TripTransferPackage source)
    {
        var copy = ClonePackage(source);
        var newTripId = Guid.NewGuid();
        var bookingIds = copy.Bookings.ToDictionary(x => x.Id, _ => Guid.NewGuid());

        copy.Trip.Id = newTripId;
        copy.Trip.Name = $"{copy.Trip.Name} (Copy)";
        copy.Trip.CreatedUtc = DateTime.UtcNow;
        copy.Trip.UpdatedUtc = copy.Trip.CreatedUtc;

        foreach (var booking in copy.Bookings)
        {
            var oldId = booking.Id;
            booking.Id = bookingIds[oldId];
            booking.TripId = newTripId;
        }

        foreach (var item in copy.ItineraryItems)
        {
            item.Id = Guid.NewGuid();
            item.TripId = newTripId;
            item.BookingId = RemapBookingId(item.BookingId, bookingIds);
        }

        foreach (var expense in copy.Expenses)
        {
            expense.Id = Guid.NewGuid();
            expense.TripId = newTripId;
            expense.BookingId = RemapBookingId(expense.BookingId, bookingIds);
        }

        foreach (var document in copy.TravelDocuments)
        {
            var oldId = document.Id;
            document.Id = Guid.NewGuid();
            document.TripId = newTripId;
            document.RelativePath = $"{newTripId:D}/{document.Id:D}/{document.FileName}";
            foreach (var wallet in copy.TravelWalletItems)
            {
                if (wallet.DocumentId == oldId) wallet.DocumentId = document.Id;
            }
        }

        foreach (var wallet in copy.TravelWalletItems)
        {
            wallet.Id = Guid.NewGuid();
            wallet.TripId = newTripId;
        }

        foreach (var checklistItem in copy.ChecklistItems)
        {
            checklistItem.Id = Guid.NewGuid();
            checklistItem.TripId = newTripId;
        }

        foreach (var preTravelItem in copy.PreTravelItems)
        {
            preTravelItem.Id = Guid.NewGuid();
            preTravelItem.TripId = newTripId;
        }

        foreach (var photo in copy.TripPhotos)
        {
            photo.Id = Guid.NewGuid();
            photo.TripId = newTripId;
        }

        foreach (var video in copy.TripVideos)
        {
            video.Id = Guid.NewGuid();
            video.TripId = newTripId;
        }

        return copy;
    }

    private static Guid? RemapBookingId(Guid? bookingId, IReadOnlyDictionary<Guid, Guid> bookingIds) =>
        bookingId.HasValue && bookingIds.TryGetValue(bookingId.Value, out var mappedId)
            ? mappedId
            : null;

    private static TripTransferPackage ClonePackage(TripTransferPackage source) => new()
    {
        PackageType = source.PackageType,
        SchemaVersion = source.SchemaVersion,
        SourceAppVersion = source.SourceAppVersion,
        ExportedUtc = source.ExportedUtc,
        Trip = Clone(source.Trip),
        ItineraryItems = source.ItineraryItems.Select(Clone).ToList(),
        Bookings = source.Bookings.Select(Clone).ToList(),
        Expenses = source.Expenses.Select(Clone).ToList(),
        ChecklistItems = source.ChecklistItems.Select(Clone).ToList(),
        PreTravelItems = source.PreTravelItems.Select(Clone).ToList(),
        TravelDocuments = source.TravelDocuments.Select(Clone).ToList(),
        TravelWalletItems = source.TravelWalletItems.Select(Clone).ToList(),
        TripPhotos = source.TripPhotos.Select(Clone).ToList(),
        TripVideos = source.TripVideos.Select(Clone).ToList(),
        CollaborationAssets = source.CollaborationAssets.Select(a => new CollaborationAsset { Id=a.Id,Kind=a.Kind,ItemId=a.ItemId,FileName=a.FileName,Sha256=a.Sha256,KeyEpoch=a.KeyEpoch,Bytes=a.Bytes,Chunks=a.Chunks }).ToList()
    };

    private static Trip Clone(Trip x) => new()
    {
        CollaborationId = x.CollaborationId,
        Id = x.Id,
        Name = x.Name,
        Destination = x.Destination,
        StartDate = x.StartDate,
        EndDate = x.EndDate,
        Budget = x.Budget,
        CurrencyCode = x.CurrencyCode,
        Status = x.Status,
        Notes = x.Notes,
        BackgroundImagePath = x.BackgroundImagePath,
        BackgroundImageSourceUrl = x.BackgroundImageSourceUrl,
        BackgroundImageCreator = x.BackgroundImageCreator,
        BackgroundImageLicense = x.BackgroundImageLicense,
        BackgroundImageLicenseUrl = x.BackgroundImageLicenseUrl,
        BackgroundImageProvider = x.BackgroundImageProvider,
        CreatedUtc = x.CreatedUtc,
        UpdatedUtc = x.UpdatedUtc
    };

    private static TravelDocument Clone(TravelDocument x) => new()
    {
        Id = x.Id, TripId = x.TripId, Title = x.Title, Category = x.Category, FileName = x.FileName,
        RelativePath = x.RelativePath, MimeType = x.MimeType, SizeBytes = x.SizeBytes, AddedUtc = x.AddedUtc, Notes = x.Notes
    };

    private static TravelWalletItem Clone(TravelWalletItem x) => new()
    {
        GoogleWalletUrl = x.GoogleWalletUrl, SamsungWalletUrl = x.SamsungWalletUrl,
        Id = x.Id, TripId = x.TripId, Category = x.Category, Title = x.Title, Reference = x.Reference, HolderName = x.HolderName,
        ValidFrom = x.ValidFrom, ValidUntil = x.ValidUntil, ContactName = x.ContactName, ContactPhone = x.ContactPhone,
        QrPayload = x.QrPayload, Notes = x.Notes, DocumentId = x.DocumentId
    };

    private static ItineraryItem Clone(ItineraryItem x) => new()
    {
        Id = x.Id,
        TripId = x.TripId,
        DayNumber = x.DayNumber,
        StartDateTime = x.StartDateTime,
        EndDateTime = x.EndDateTime,
        Title = x.Title,
        Location = x.Location,
        Website = x.Website ?? string.Empty,
        Notes = x.Notes,
        Category = x.Category,
        BookingId = x.BookingId,
        SortOrder = x.SortOrder
    };

    private static Booking Clone(Booking x) => new()
    {
        GoogleWalletUrl = x.GoogleWalletUrl, SamsungWalletUrl = x.SamsungWalletUrl,
        Id = x.Id,
        TripId = x.TripId,
        Type = x.Type,
        Name = x.Name,
        Provider = x.Provider,
        Reference = x.Reference,
        Address = x.Address,
        Phone = x.Phone,
        Website = x.Website,
        CheckIn = x.CheckIn,
        CheckOut = x.CheckOut,
        TotalCost = x.TotalCost,
        AmountPaid = x.AmountPaid,
        CurrencyCode = x.CurrencyCode,
        ExchangeRateToBase = x.ExchangeRateToBase,
        Status = x.Status,
        Notes = x.Notes
    };

    private static Expense Clone(Expense x) => new()
    {
        Id = x.Id,
        TripId = x.TripId,
        BookingId = x.BookingId,
        Date = x.Date,
        Category = x.Category,
        Description = x.Description,
        Amount = x.Amount,
        CurrencyCode = x.CurrencyCode,
        ExchangeRateToBase = x.ExchangeRateToBase,
        Notes = x.Notes
    };

    private static ChecklistItem Clone(ChecklistItem x) => new()
    {
        Id = x.Id,
        TripId = x.TripId,
        ListName = x.ListName,
        Text = x.Text,
        IsCompleted = x.IsCompleted,
        SortOrder = x.SortOrder
    };

    private static PreTravelItem Clone(PreTravelItem x) => new()
    {
        Id = x.Id,
        TripId = x.TripId,
        Category = x.Category,
        Text = x.Text,
        IsCompleted = x.IsCompleted,
        DueDate = x.DueDate,
        Notes = x.Notes,
        SortOrder = x.SortOrder
    };

    private sealed class SqliteColumnInfo
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class SqliteTableInfo
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }

    private async Task MigrateLegacySchemaAsync()
    {
        // Existing pre-obfuscation databases are migrated once. New databases never
        // contain the old semantic table/column names.
        var tables = await _database.QueryAsync<SqliteTableInfo>(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';")
            .ConfigureAwait(false);
        var existing = tables.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);

        var mappings = new[]
        {
            (DatabaseSchema.A1, typeof(TripEntity)),
            (DatabaseSchema.A2, typeof(ItineraryItemEntity)),
            (DatabaseSchema.A3, typeof(BookingEntity)),
            (DatabaseSchema.A4, typeof(ExpenseEntity)),
            (DatabaseSchema.A5, typeof(ChecklistItemEntity)),
            (DatabaseSchema.A6, typeof(TravelDocumentEntity)),
            (DatabaseSchema.A7, typeof(TravelWalletItemEntity)),
            (DatabaseSchema.A8, typeof(TripPhotoEntity))
        };

        foreach (var (newTable, entityType) in mappings)
        {
            if (existing.Contains(newTable))
                continue;

            if (!DatabaseSchema.LegacyTables.TryGetValue(newTable, out var encodedLegacyName))
                continue;

            var legacyTable = DatabaseSchema.DecodeLegacyName(encodedLegacyName);
            if (!existing.Contains(legacyTable))
                continue;

            await _database.RunInTransactionAsync(connection =>
            {
                connection.Execute($"ALTER TABLE {DatabaseSchema.Quote(legacyTable)} RENAME TO {DatabaseSchema.Quote(newTable)};");
            }).ConfigureAwait(false);

            var columns = await _database.QueryAsync<SqliteColumnInfo>(
                $"PRAGMA table_info({DatabaseSchema.Quote(newTable)});").ConfigureAwait(false);
            var oldColumns = columns.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var property in entityType.GetProperties())
            {
                if (!oldColumns.Contains(property.Name))
                    continue;

                var newColumn = DatabaseSchema.ColumnFor(property.Name);
                if (string.Equals(property.Name, newColumn, StringComparison.Ordinal))
                    continue;

                await _database.ExecuteAsync(
                    $"ALTER TABLE {DatabaseSchema.Quote(newTable)} RENAME COLUMN {DatabaseSchema.Quote(property.Name)} TO {DatabaseSchema.Quote(newColumn)};")
                    .ConfigureAwait(false);
            }

            existing.Remove(legacyTable);
            existing.Add(newTable);
        }
    }

    private async Task EnsureTripBackgroundColumnsAsync()
    {
        var columns = await _database.QueryAsync<SqliteColumnInfo>(
            $"PRAGMA table_info({DatabaseSchema.Quote(DatabaseSchema.A1)});").ConfigureAwait(false);
        var existing = columns.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var additions = new[]
        {
            ("c_shared_origin", "TEXT"),
            (DatabaseSchema.B11, "TEXT"),
            (DatabaseSchema.B12, "TEXT"),
            (DatabaseSchema.B13, "TEXT"),
            (DatabaseSchema.B14, "TEXT"),
            (DatabaseSchema.B15, "TEXT"),
            (DatabaseSchema.B16, "TEXT")
        };

        foreach (var (name, type) in additions)
        {
            if (!existing.Contains(name))
                await _database.ExecuteAsync(
                    $"ALTER TABLE {DatabaseSchema.Quote(DatabaseSchema.A1)} ADD COLUMN {DatabaseSchema.Quote(name)} {type} DEFAULT '';" )
                    .ConfigureAwait(false);
        }
    }

    private static Trip ToModel(TripEntity x) => new()
    {
        Id = Guid.Parse(x.Id), CollaborationId = x.CollaborationId ?? string.Empty, Name = x.Name, Destination = x.Destination,
        StartDate = x.StartDate, EndDate = x.EndDate, Budget = x.Budget,
        CurrencyCode = x.CurrencyCode, Status = (TripStatus)x.Status, Notes = x.Notes,
        BackgroundImagePath = x.BackgroundImagePath ?? string.Empty,
        BackgroundImageSourceUrl = x.BackgroundImageSourceUrl ?? string.Empty,
        BackgroundImageCreator = x.BackgroundImageCreator ?? string.Empty,
        BackgroundImageLicense = x.BackgroundImageLicense ?? string.Empty,
        BackgroundImageLicenseUrl = x.BackgroundImageLicenseUrl ?? string.Empty,
        BackgroundImageProvider = x.BackgroundImageProvider ?? string.Empty,
        CreatedUtc = x.CreatedUtc, UpdatedUtc = x.UpdatedUtc
    };

    private static TripEntity ToEntity(Trip x) => new()
    {
        Id = x.Id.ToString("D"), CollaborationId = x.CollaborationId, Name = x.Name.Trim(), Destination = x.Destination.Trim(),
        StartDate = x.StartDate, EndDate = x.EndDate, Budget = x.Budget,
        CurrencyCode = x.CurrencyCode, Status = (int)x.Status, Notes = x.Notes,
        BackgroundImagePath = x.BackgroundImagePath ?? string.Empty,
        BackgroundImageSourceUrl = x.BackgroundImageSourceUrl ?? string.Empty,
        BackgroundImageCreator = x.BackgroundImageCreator ?? string.Empty,
        BackgroundImageLicense = x.BackgroundImageLicense ?? string.Empty,
        BackgroundImageLicenseUrl = x.BackgroundImageLicenseUrl ?? string.Empty,
        BackgroundImageProvider = x.BackgroundImageProvider ?? string.Empty,
        CreatedUtc = x.CreatedUtc, UpdatedUtc = x.UpdatedUtc
    };

    private static ItineraryItem ToModel(ItineraryItemEntity x) => new()
    {
        Id = Guid.Parse(x.Id), TripId = Guid.Parse(x.TripId), DayNumber = x.DayNumber,
        StartDateTime = x.StartDateTime, EndDateTime = x.EndDateTime, Title = x.Title,
        Location = x.Location, Website = x.Website ?? string.Empty, Notes = x.Notes, Category = (ItineraryCategory)x.Category,
        BookingId = ParseNullableGuid(x.BookingId), SortOrder = x.SortOrder
    };

    private static ItineraryItemEntity ToEntity(ItineraryItem x) => new()
    {
        Id = x.Id.ToString("D"), TripId = x.TripId.ToString("D"), DayNumber = x.DayNumber,
        StartDateTime = x.StartDateTime, EndDateTime = x.EndDateTime, Title = x.Title.Trim(),
        Location = x.Location.Trim(), Website = (x.Website ?? string.Empty).Trim(), Notes = x.Notes, Category = (int)x.Category,
        BookingId = x.BookingId?.ToString("D"), SortOrder = x.SortOrder
    };

    private static Booking ToModel(BookingEntity x) => new()
    {
        GoogleWalletUrl = x.GoogleWalletUrl ?? string.Empty, SamsungWalletUrl = x.SamsungWalletUrl ?? string.Empty,
        Id = Guid.Parse(x.Id), TripId = Guid.Parse(x.TripId), Type = (BookingType)x.Type,
        Name = x.Name, Provider = x.Provider, Reference = x.Reference, Address = x.Address,
        Phone = x.Phone, Website = x.Website, CheckIn = x.CheckIn, CheckOut = x.CheckOut,
        TotalCost = x.TotalCost, AmountPaid = x.AmountPaid, CurrencyCode = x.CurrencyCode,
        ExchangeRateToBase = x.ExchangeRateToBase <= 0 ? 1m : x.ExchangeRateToBase, Status = (BookingStatus)x.Status, Notes = x.Notes
    };

    private static BookingEntity ToEntity(Booking x) => new()
    {
        GoogleWalletUrl = (x.GoogleWalletUrl ?? string.Empty).Trim(), SamsungWalletUrl = (x.SamsungWalletUrl ?? string.Empty).Trim(),
        Id = x.Id.ToString("D"), TripId = x.TripId.ToString("D"), Type = (int)x.Type,
        Name = x.Name.Trim(), Provider = x.Provider.Trim(), Reference = x.Reference.Trim(),
        Address = x.Address.Trim(), Phone = x.Phone.Trim(), Website = x.Website.Trim(),
        CheckIn = x.CheckIn, CheckOut = x.CheckOut, TotalCost = x.TotalCost,
        AmountPaid = x.AmountPaid, CurrencyCode = x.CurrencyCode, ExchangeRateToBase = x.ExchangeRateToBase <= 0 ? 1m : x.ExchangeRateToBase, Status = (int)x.Status,
        Notes = x.Notes
    };

    private static Expense ToModel(ExpenseEntity x) => new()
    {
        Id = Guid.Parse(x.Id), TripId = Guid.Parse(x.TripId), BookingId = ParseNullableGuid(x.BookingId),
        Date = x.Date, Category = x.Category, Description = x.Description, Amount = x.Amount,
        CurrencyCode = x.CurrencyCode, ExchangeRateToBase = x.ExchangeRateToBase <= 0 ? 1m : x.ExchangeRateToBase, Notes = x.Notes
    };

    private static ExpenseEntity ToEntity(Expense x) => new()
    {
        Id = x.Id.ToString("D"), TripId = x.TripId.ToString("D"), BookingId = x.BookingId?.ToString("D"),
        Date = x.Date, Category = x.Category.Trim(), Description = x.Description.Trim(),
        Amount = x.Amount, CurrencyCode = x.CurrencyCode, ExchangeRateToBase = x.ExchangeRateToBase <= 0 ? 1m : x.ExchangeRateToBase, Notes = x.Notes
    };

    private static TravelDocumentEntity ToEntity(TravelDocument x) => new()
    {
        Id = x.Id.ToString("D"), TripId = x.TripId.ToString("D"), Title = x.Title, Category = x.Category, FileName = x.FileName,
        RelativePath = x.RelativePath, MimeType = x.MimeType, SizeBytes = x.SizeBytes, AddedUtc = x.AddedUtc, Notes = x.Notes
    };

    private static TravelDocument ToModel(TravelDocumentEntity x) => new()
    {
        Id = Guid.Parse(x.Id), TripId = Guid.Parse(x.TripId), Title = x.Title, Category = x.Category, FileName = x.FileName,
        RelativePath = x.RelativePath, MimeType = x.MimeType, SizeBytes = x.SizeBytes, AddedUtc = x.AddedUtc, Notes = x.Notes
    };

    private static TravelWalletItemEntity ToEntity(TravelWalletItem x) => new()
    {
        GoogleWalletUrl = (x.GoogleWalletUrl ?? string.Empty).Trim(), SamsungWalletUrl = (x.SamsungWalletUrl ?? string.Empty).Trim(),
        Id = x.Id.ToString("D"), TripId = x.TripId.ToString("D"), Category = (int)x.Category, Title = x.Title, Reference = x.Reference,
        HolderName = x.HolderName, ValidFrom = x.ValidFrom, ValidUntil = x.ValidUntil, ContactName = x.ContactName, ContactPhone = x.ContactPhone,
        QrPayload = x.QrPayload, Notes = x.Notes, DocumentId = x.DocumentId?.ToString("D")
    };

    private static TravelWalletItem ToModel(TravelWalletItemEntity x) => new()
    {
        GoogleWalletUrl = x.GoogleWalletUrl ?? string.Empty, SamsungWalletUrl = x.SamsungWalletUrl ?? string.Empty,
        Id = Guid.Parse(x.Id), TripId = Guid.Parse(x.TripId), Category = Enum.IsDefined(typeof(TravelWalletCategory), x.Category) ? (TravelWalletCategory)x.Category : TravelWalletCategory.Other,
        Title = x.Title, Reference = x.Reference, HolderName = x.HolderName, ValidFrom = x.ValidFrom, ValidUntil = x.ValidUntil,
        ContactName = x.ContactName, ContactPhone = x.ContactPhone, QrPayload = x.QrPayload, Notes = x.Notes,
        DocumentId = Guid.TryParse(x.DocumentId, out var id) ? id : null
    };

    private static ChecklistItem ToModel(ChecklistItemEntity x) => new()
    {
        Id = Guid.Parse(x.Id), TripId = Guid.Parse(x.TripId), ListName = x.ListName,
        Text = x.Text, IsCompleted = x.IsCompleted, SortOrder = x.SortOrder
    };

    private static ChecklistItemEntity ToEntity(ChecklistItem x) => new()
    {
        Id = x.Id.ToString("D"), TripId = x.TripId.ToString("D"), ListName = x.ListName.Trim(),
        Text = x.Text.Trim(), IsCompleted = x.IsCompleted, SortOrder = x.SortOrder
    };

    private static PreTravelItem ToModel(PreTravelItemEntity x) => new()
    {
        Id = Guid.Parse(x.Id), TripId = Guid.Parse(x.TripId), Category = x.Category, Text = x.Text,
        IsCompleted = x.IsCompleted, DueDate = x.DueDate, Notes = x.Notes, SortOrder = x.SortOrder
    };

    private static PreTravelItemEntity ToEntity(PreTravelItem x) => new()
    {
        Id = x.Id.ToString("D"), TripId = x.TripId.ToString("D"),
        Category = string.IsNullOrWhiteSpace(x.Category) ? "General" : x.Category.Trim(),
        Text = x.Text.Trim(), IsCompleted = x.IsCompleted, DueDate = x.DueDate.Date,
        Notes = x.Notes?.Trim() ?? string.Empty, SortOrder = x.SortOrder
    };

    private static TripTransferVideo Clone(TripTransferVideo x) => new()
    {
        Id = x.Id, TripId = x.TripId, FileName = x.FileName, AddedUtc = x.AddedUtc, CapturedDate = x.CapturedDate
    };

    private static TripPhoto Clone(TripPhoto x) => new()
    {
        Id = x.Id, TripId = x.TripId, FilePath = x.FilePath, FileName = x.FileName,
        CapturedDate = x.CapturedDate, Caption = x.Caption, TakenUtc = x.TakenUtc, AddedUtc = x.AddedUtc, SortOrder = x.SortOrder,
        EmbeddedContentBase64 = x.EmbeddedContentBase64
    };

    private static TripPhotoEntity ToEntity(TripPhoto x) => new()
    {
        Id = x.Id.ToString("D"), TripId = x.TripId.ToString("D"), FilePath = x.FilePath,
        FileName = x.FileName, CapturedDate = x.CapturedDate, Caption = x.Caption, TakenUtc = x.TakenUtc, AddedUtc = x.AddedUtc, SortOrder = x.SortOrder
    };

    private static TripPhoto ToModel(TripPhotoEntity x) => new()
    {
        Id = Guid.Parse(x.Id), TripId = Guid.Parse(x.TripId), FilePath = x.FilePath ?? string.Empty,
        FileName = x.FileName ?? string.Empty, CapturedDate = x.CapturedDate, Caption = x.Caption ?? string.Empty, TakenUtc = x.TakenUtc,
        AddedUtc = x.AddedUtc, SortOrder = x.SortOrder
    };

    private static Guid? ParseNullableGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
