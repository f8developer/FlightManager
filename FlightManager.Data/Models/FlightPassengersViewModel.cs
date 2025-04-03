namespace FlightManager.Data.Models
{
    public class FlightPassengersViewModel
    {
        public Flight Flight { get; set; }
        public PaginatedList<Reservation> PaginatedReservations { get; set; }
    }
}