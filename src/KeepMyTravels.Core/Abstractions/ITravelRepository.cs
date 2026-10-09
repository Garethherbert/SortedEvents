using KeepMyTravels.Core.Models;

namespace KeepMyTravels.Core.Abstractions;

public interface ITravelRepository
{
    Task InitializeAsync();

    Task<IReadOnlyList<Trip>> GetTripsAsync();
    Task<Trip?> GetTripAsync(Guid tripId);
    Task UpsertTripAsync(Trip trip);
    Task UpsertTripFromSyncAsync(Trip trip);
    Task DeleteTripAsync(Guid tripId);

    Task<IReadOnlyList<ItineraryItem>> GetItineraryAsync(Guid tripId);
    Task<ItineraryItem?> GetItineraryItemAsync(Guid itemId);
    Task UpsertItineraryItemAsync(ItineraryItem item);
    Task DeleteItineraryItemAsync(Guid itemId);

    Task<IReadOnlyList<Booking>> GetBookingsAsync(Guid tripId);
    Task<Booking?> GetBookingAsync(Guid bookingId);
    Task UpsertBookingAsync(Booking booking);
    Task DeleteBookingAsync(Guid bookingId);

    Task<IReadOnlyList<Expense>> GetExpensesAsync(Guid tripId);
    Task<Expense?> GetExpenseAsync(Guid expenseId);
    Task UpsertExpenseAsync(Expense expense);
    Task DeleteExpenseAsync(Guid expenseId);

    Task<IReadOnlyList<ChecklistItem>> GetChecklistItemsAsync(Guid tripId);
    Task UpsertChecklistItemAsync(ChecklistItem item);
    Task DeleteChecklistItemAsync(Guid itemId);

    Task<IReadOnlyList<PreTravelItem>> GetPreTravelItemsAsync(Guid tripId);
    Task UpsertPreTravelItemAsync(PreTravelItem item);
    Task DeletePreTravelItemAsync(Guid itemId);

    Task<IReadOnlyList<TravelDocument>> GetTravelDocumentsAsync(Guid tripId);
    Task<TravelDocument?> GetTravelDocumentAsync(Guid documentId);
    Task UpsertTravelDocumentAsync(TravelDocument document);
    Task DeleteTravelDocumentAsync(Guid documentId);

    Task<IReadOnlyList<TravelWalletItem>> GetTravelWalletItemsAsync(Guid tripId);
    Task<TravelWalletItem?> GetTravelWalletItemAsync(Guid itemId);
    Task UpsertTravelWalletItemAsync(TravelWalletItem item);
    Task DeleteTravelWalletItemAsync(Guid itemId);

    Task<IReadOnlyList<TripPhoto>> GetTripPhotosAsync(Guid tripId);
    Task UpsertTripPhotoAsync(TripPhoto photo);
    Task DeleteTripPhotoAsync(Guid photoId);
}
