namespace FlightManager.Data.Models;

/// <summary>
/// ViewModel for displaying flight details along with paginated passenger reservations.
/// Combines flight information with a paginated list of reservations for that flight.
/// </summary>
public class FlightPassengersViewModel
{
    /// <summary>
    /// Gets or sets the flight details including route, times, and capacity information.
    /// </summary>
    public Flight Flight { get; set; }

    /// <summary>
    /// Gets or sets the paginated list of reservations for the flight,
    /// containing passenger details and booking information.
    /// </summary>
    public PaginatedList<Reservation> PaginatedReservations { get; set; }
}