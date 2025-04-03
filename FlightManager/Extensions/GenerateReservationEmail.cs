using System.Globalization;
using FlightManager.Data.Models;
using System.Text;
using FlightManager.Extensions.Services;

namespace FlightManager.Extensions
{
    /// <summary>
    /// Provides methods for generating HTML email content for flight reservations.
    /// </summary>
    public static class GenerateReservationEmail
    {
        /// <summary>
        /// Generates an HTML email for an individual flight reservation confirmation.
        /// </summary>
        /// <param name="reservation">The reservation details.</param>
        /// <param name="flight">The flight information.</param>
        /// <param name="user">The reservation user details.</param>
        /// <param name="confirmationUrl">The URL for confirming the reservation.</param>
        /// <param name="detailsUrl">The URL for viewing reservation details.</param>
        /// <param name="templateService">The email template service.</param>
        /// <returns>An HTML-formatted email string with populated reservation data.</returns>
        public static string ReservationEmail(
            Reservation reservation,
            Flight flight,
            ReservationUser user,
            string confirmationUrl,
            string detailsUrl,
            EmailTemplateService templateService)
        {
            var duration = flight.ArrivalTime - flight.DepartureTime;
            var formattedDuration = FormatDuration(duration);
            var expiresAt = DateTime.UtcNow.AddDays(2).ToString("f", CultureInfo.InvariantCulture);
            var ticketType = reservation.TicketType == TicketType.Business ? "Business Class" : "Economy Class";

            var replacements = new Dictionary<string, string>
            {
                {"ReservationId", reservation.Id.ToString()},
                {"FirstName", user.FirstName ?? "Passenger"},
                {"LastName", user.LastName ?? string.Empty},
                {"Nationality", reservation.Nationality ?? "Not specified"},
                {"TicketType", ticketType},
                {"FlightNumber", flight.AircraftNumber},
                {"FromLocation", flight.FromLocation},
                {"ToLocation", flight.ToLocation},
                {"DepartureTime", flight.DepartureTime.ToString("f", CultureInfo.InvariantCulture)},
                {"ArrivalTime", flight.ArrivalTime.ToString("f", CultureInfo.InvariantCulture)},
                {"Duration", formattedDuration},
                {"ConfirmationUrl", confirmationUrl},
                {"DetailsUrl", detailsUrl},
                {"ExpiresAt", expiresAt},
                {"CurrentYear", DateTime.Now.Year.ToString()}
            };

            return templateService.GetTemplate("ReservationConfirmation.html", replacements);
        }

        /// <summary>
        /// Generates an HTML email for a group flight reservation confirmation.
        /// </summary>
        /// <param name="reservations">List of reservations in the group.</param>
        /// <param name="flight">The flight information.</param>
        /// <param name="passengers">List of passenger details.</param>
        /// <param name="confirmationUrl">The URL for confirming the group reservation.</param>
        /// <param name="detailsUrl">The base URL for viewing reservation details.</param>
        /// <param name="templateService">The email template service.</param>
        /// <returns>An HTML-formatted email string with populated group reservation data.</returns>
        /// <exception cref="ArgumentException">Thrown when reservations or passengers lists are empty or counts don't match.</exception>
        public static string GroupReservationEmail(
            List<Reservation> reservations,
            Flight flight,
            List<PassengerViewModel> passengers,
            string confirmationUrl,
            string detailsUrl,
            EmailTemplateService templateService)
        {
            if (reservations == null || !reservations.Any())
                throw new ArgumentException("Reservations list cannot be empty", nameof(reservations));

            if (passengers == null || !passengers.Any())
                throw new ArgumentException("Passengers list cannot be empty", nameof(passengers));

            if (reservations.Count != passengers.Count)
                throw new ArgumentException("Reservations and passengers counts must match");

            var duration = flight.ArrivalTime - flight.DepartureTime;
            var formattedDuration = FormatDuration(duration);
            var expiresAt = DateTime.UtcNow.AddDays(2).ToString("f", CultureInfo.InvariantCulture);
            var mainReservation = reservations.First();
            var ticketType = mainReservation.TicketType == TicketType.Business ? "Business Class" : "Economy Class";

            // Generate passenger rows with status indication
            var passengerRows = new StringBuilder();
            foreach (var (passenger, i) in passengers.Select((p, i) => (p, i)))
            {
                var reservation = reservations[i];
                var statusBadge = reservation.IsConfirmed
                    ? "<span class='badge bg-success'>Confirmed</span>"
                    : $"<span class='badge bg-warning'>Pending (<span class='countdown' data-expiry='{reservation.CreatedAt.AddHours(48):O}'></span>)</span>";

                passengerRows.AppendLine($@"
                <tr>
                    <td style='padding: 0.75rem; border-bottom: 1px solid #dee2e6;'>{passenger.FirstName} {passenger.LastName}</td>
                    <td style='padding: 0.75rem; border-bottom: 1px solid #dee2e6;'>#{reservation.Id}</td>
                    <td style='padding: 0.75rem; border-bottom: 1px solid #dee2e6;'>
                        <span class='badge {(reservation.TicketType == TicketType.Business ? "bg-primary" : "bg-secondary")}'>
                            {reservation.TicketType}
                        </span>
                    </td>
                    <td style='padding: 0.75rem; border-bottom: 1px solid #dee2e6;'>
                        {statusBadge}
                    </td>
                </tr>");
            }

            var replacements = new Dictionary<string, string>
            {
                {"MainReservationId", mainReservation.Id.ToString()},
                {"PassengerCount", passengers.Count.ToString()},
                {"TicketType", ticketType},
                {"FlightNumber", flight.AircraftNumber},
                {"FromLocation", flight.FromLocation},
                {"ToLocation", flight.ToLocation},
                {"DepartureTime", flight.DepartureTime.ToString("f", CultureInfo.InvariantCulture)},
                {"ArrivalTime", flight.ArrivalTime.ToString("f", CultureInfo.InvariantCulture)},
                {"Duration", formattedDuration},
                {"ConfirmationUrl", confirmationUrl},
                {"DetailsUrl", detailsUrl},
                {"ExpiresAt", expiresAt},
                {"CurrentYear", DateTime.Now.Year.ToString()},
                {"PassengerRows", passengerRows.ToString()}
            };

            return templateService.GetTemplate("GroupReservationConfirmation.html", replacements);
        }

        /// <summary>
        /// Formats a TimeSpan duration into a human-readable string.
        /// </summary>
        /// <param name="duration">The time duration to format.</param>
        /// <returns>
        /// A formatted string in "Xd Xh Xm" format for durations over 24 hours,
        /// or "Xh Xm" format for shorter durations.
        /// </returns>
        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 24)
            {
                return $"{duration.Days} day{(duration.Days > 1 ? "s" : "")} {duration.Hours}h {duration.Minutes}m";
            }
            return $"{duration.Hours}h {duration.Minutes}m";
        }
    }
}