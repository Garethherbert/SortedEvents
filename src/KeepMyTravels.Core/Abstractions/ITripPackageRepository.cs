using KeepMyTravels.Core.Enums;
using KeepMyTravels.Core.Transfer;

namespace KeepMyTravels.Core.Abstractions;

public interface ITripPackageRepository
{
    Task<TripTransferPackage?> CreateTripPackageAsync(Guid tripId, string sourceAppVersion);
    Task<bool> TripExistsAsync(Guid tripId);
    Task<Guid> ImportTripPackageAsync(TripTransferPackage package, TripImportMode mode);
    Task<Guid> ReplaceAllWithTripPackageAsync(TripTransferPackage package);
}
