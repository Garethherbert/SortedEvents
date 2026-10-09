namespace KeepMyTravels.Core.Services;

public static class TripMediaDates
{
    public static int? DayNumber(DateTime captureDate, DateTime startDate, DateTime endDate) =>
        captureDate.Date < startDate.Date || captureDate.Date > endDate.Date
            ? null : (captureDate.Date - startDate.Date).Days + 1;
}
