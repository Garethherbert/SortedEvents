using KeepMyTravels.Core.Enums;

namespace KeepMyTravels.Core.Transfer;

public static class TripTransferPackageValidator
{
    public static void Validate(TripTransferPackage? package)
    {
        if (package is null)
        {
            throw new InvalidDataException("The selected file does not contain a KeepMyTravels trip.");
        }

        if (!string.Equals(
                package.PackageType,
                TripTransferPackage.CurrentPackageType,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException("The selected file is not a KeepMyTravels trip export.");
        }

        if (package.SchemaVersion < 1)
        {
            throw new InvalidDataException("The trip file has an invalid schema version.");
        }

        if (package.SchemaVersion > TripTransferPackage.CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"This trip was exported by a newer version of KeepMyTravels (schema {package.SchemaVersion}). Update the app before importing it.");
        }

        package.ItineraryItems ??= [];
        package.Bookings ??= [];
        package.Expenses ??= [];
        package.ChecklistItems ??= [];
        package.PreTravelItems ??= [];
        package.TravelDocuments ??= [];
        package.TravelWalletItems ??= [];
        package.TripPhotos ??= [];
        package.TripVideos ??= [];
        package.CollaborationAssets ??= [];

        var trip = package.Trip ?? throw new InvalidDataException("The trip file is missing its trip record.");
        if (trip.Id == Guid.Empty)
        {
            throw new InvalidDataException("The trip file contains an invalid trip identifier.");
        }

        if (string.IsNullOrWhiteSpace(trip.Name))
        {
            throw new InvalidDataException("The imported trip does not have a name.");
        }

        if (trip.EndDate.Date < trip.StartDate.Date)
        {
            throw new InvalidDataException("The imported trip ends before it starts.");
        }

        if (!Enum.IsDefined(trip.Status))
        {
            throw new InvalidDataException("The imported trip contains an unsupported trip status.");
        }

        EnsureUniqueIds(package.ItineraryItems.Select(x => x.Id), "itinerary");
        EnsureUniqueIds(package.Bookings.Select(x => x.Id), "booking");
        EnsureUniqueIds(package.Expenses.Select(x => x.Id), "expense");
        EnsureUniqueIds(package.ChecklistItems.Select(x => x.Id), "checklist");
        EnsureUniqueIds(package.PreTravelItems.Select(x => x.Id), "pre-travel");
        EnsureUniqueIds(package.TravelDocuments.Select(x => x.Id), "document");
        EnsureUniqueIds(package.TravelWalletItems.Select(x => x.Id), "wallet");
        EnsureUniqueIds(package.TripPhotos.Select(x => x.Id), "photo");
        EnsureUniqueIds(package.TripVideos.Select(x => x.Id), "video");

        var bookingIds = package.Bookings.Select(x => x.Id).ToHashSet();

        foreach (var item in package.ItineraryItems)
        {
            item.Website ??= string.Empty;
            EnsureChild(item.Id, item.TripId, trip.Id, "itinerary");
            if (string.IsNullOrWhiteSpace(item.Title))
            {
                throw new InvalidDataException("An itinerary item is missing its title.");
            }
            if (!Enum.IsDefined(item.Category))
            {
                throw new InvalidDataException($"Itinerary item '{item.Title}' contains an unsupported category.");
            }
            EnsureBookingReference(item.BookingId, bookingIds, $"Itinerary item '{item.Title}'");
        }

        foreach (var booking in package.Bookings)
        {
            EnsureChild(booking.Id, booking.TripId, trip.Id, "booking");
            if (string.IsNullOrWhiteSpace(booking.Name))
            {
                throw new InvalidDataException("A booking is missing its name.");
            }
            if (!Enum.IsDefined(booking.Type) || !Enum.IsDefined(booking.Status))
            {
                throw new InvalidDataException($"Booking '{booking.Name}' contains an unsupported type or status.");
            }
        }

        foreach (var expense in package.Expenses)
        {
            EnsureChild(expense.Id, expense.TripId, trip.Id, "expense");
            EnsureBookingReference(expense.BookingId, bookingIds, $"Expense '{expense.Description}'");
        }

        foreach (var checklistItem in package.ChecklistItems)
        {
            EnsureChild(checklistItem.Id, checklistItem.TripId, trip.Id, "checklist");
            if (string.IsNullOrWhiteSpace(checklistItem.Text))
            {
                throw new InvalidDataException("A checklist item is missing its description.");
            }
        }

        foreach (var preTravelItem in package.PreTravelItems)
        {
            EnsureChild(preTravelItem.Id, preTravelItem.TripId, trip.Id, "pre-travel");
            if (string.IsNullOrWhiteSpace(preTravelItem.Text))
            {
                throw new InvalidDataException("A pre-travel task is missing its description.");
            }
            if (string.IsNullOrWhiteSpace(preTravelItem.Category))
            {
                preTravelItem.Category = "General";
            }
        }

        var documentIds = package.TravelDocuments.Select(x => x.Id).ToHashSet();
        long embeddedDocumentBytes = 0;
        const long maximumEmbeddedDocumentBytes = 15L * 1024L * 1024L;
        const long maximumSingleDocumentBytes = 20L * 1024L * 1024L;
        foreach (var document in package.TravelDocuments)
        {
            EnsureChild(document.Id, document.TripId, trip.Id, "document");
            if (string.IsNullOrWhiteSpace(document.Title) || string.IsNullOrWhiteSpace(document.FileName))
                throw new InvalidDataException("A travel document is missing its title or file name.");
            if (document.SizeBytes < 0 || document.SizeBytes > maximumSingleDocumentBytes)
                throw new InvalidDataException($"Travel document '{document.Title}' has an invalid file size.");
            if (!string.IsNullOrWhiteSpace(document.ContentBase64))
            {
                try
                {
                    var bytes = Convert.FromBase64String(document.ContentBase64);
                    if (bytes.LongLength > maximumSingleDocumentBytes)
                        throw new InvalidDataException($"Travel document '{document.Title}' is larger than 20 MB.");
                    embeddedDocumentBytes += bytes.LongLength;
                    if (embeddedDocumentBytes > maximumEmbeddedDocumentBytes)
                        throw new InvalidDataException("Embedded travel documents exceed the 15 MB transfer limit.");
                    if (document.SizeBytes != 0 && document.SizeBytes != bytes.LongLength)
                        throw new InvalidDataException($"Travel document '{document.Title}' has inconsistent attachment metadata.");
                }
                catch (FormatException ex)
                {
                    throw new InvalidDataException($"Travel document '{document.Title}' contains invalid attachment data.", ex);
                }
            }
        }

        long embeddedPhotoBytes = 0;
        const long maximumEmbeddedPhotoBytes = 15L * 1024L * 1024L;
        const long maximumSinglePhotoBytes = 10L * 1024L * 1024L;
        foreach (var photo in package.TripPhotos)
        {
            EnsureChild(photo.Id, photo.TripId, trip.Id, "photo");
            if (string.IsNullOrWhiteSpace(photo.FileName))
                throw new InvalidDataException("A trip photo is missing its file name.");
            if (!string.IsNullOrWhiteSpace(photo.EmbeddedContentBase64))
            {
                try
                {
                    var bytes = Convert.FromBase64String(photo.EmbeddedContentBase64);
                    if (bytes.LongLength > maximumSinglePhotoBytes)
                        throw new InvalidDataException($"Trip photo '{photo.FileName}' is larger than 10 MB.");
                    embeddedPhotoBytes += bytes.LongLength;
                    if (embeddedPhotoBytes > maximumEmbeddedPhotoBytes)
                        throw new InvalidDataException("Embedded trip photos exceed the 15 MB transfer limit.");
                }
                catch (FormatException ex)
                {
                    throw new InvalidDataException($"Trip photo '{photo.FileName}' contains invalid attachment data.", ex);
                }
            }
        }

        foreach (var video in package.TripVideos)
        {
            EnsureChild(video.Id, video.TripId, trip.Id, "video");
            if (string.IsNullOrWhiteSpace(video.FileName))
                throw new InvalidDataException("A trip video is missing its file name.");
        }

        foreach (var wallet in package.TravelWalletItems)
        {
            EnsureChild(wallet.Id, wallet.TripId, trip.Id, "wallet");
            if (!Enum.IsDefined(wallet.Category)) throw new InvalidDataException($"Travel wallet item '{wallet.Title}' contains an unsupported category.");
            if (string.IsNullOrWhiteSpace(wallet.Title)) throw new InvalidDataException("A travel wallet item is missing its title.");
            if (wallet.DocumentId.HasValue && !documentIds.Contains(wallet.DocumentId.Value))
                throw new InvalidDataException($"Travel wallet item '{wallet.Title}' refers to a document that is not included in the trip file.");
            if (wallet.ValidUntil.HasValue && wallet.ValidFrom.HasValue && wallet.ValidUntil < wallet.ValidFrom)
                throw new InvalidDataException($"Travel wallet item '{wallet.Title}' has an invalid validity range.");
        }
    }

    private static void EnsureUniqueIds(IEnumerable<Guid> ids, string itemType)
    {
        var seen = new HashSet<Guid>();
        foreach (var id in ids)
        {
            if (id == Guid.Empty)
            {
                throw new InvalidDataException($"The trip file contains an invalid {itemType} identifier.");
            }
            if (!seen.Add(id))
            {
                throw new InvalidDataException($"The trip file contains a duplicate {itemType} identifier.");
            }
        }
    }

    private static void EnsureChild(Guid id, Guid actualTripId, Guid expectedTripId, string itemType)
    {
        if (id == Guid.Empty || actualTripId != expectedTripId)
        {
            throw new InvalidDataException($"The trip file contains an invalid {itemType} record.");
        }
    }

    private static void EnsureBookingReference(Guid? bookingId, HashSet<Guid> bookingIds, string owner)
    {
        if (bookingId.HasValue && !bookingIds.Contains(bookingId.Value))
        {
            throw new InvalidDataException($"{owner} refers to a booking that is not included in the trip file.");
        }
    }
}
