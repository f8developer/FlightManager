using FlightManager.Data.Models;

namespace FlightManager.Extensions
{
    public class GenerateReservationEmail
    {
        public static string ReservationEmail(Reservation reservation, Flight flight, ReservationUser user)
        {
            var duration = flight.ArrivalTime - flight.DepartureTime;

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .card {{ border: 1px solid #ddd; border-radius: 5px; margin-bottom: 20px; }}
        .card-header {{ background-color: #f8f9fa; padding: 10px 15px; border-bottom: 1px solid #ddd; }}
        .card-body {{ padding: 15px; }}
        .footer {{ text-align: center; padding: 20px; font-size: 0.9em; color: #6c757d; }}
        .reservation-id {{ font-size: 2em; color: #dc3545; text-align: center; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Flight Reservation Confirmation</h1>
    </div>
    
    <div class='content'>
        <p>Dear {user.UserName},</p>
        <p>Your flight reservation has been successfully confirmed. Below are your reservation details:</p>
        
        <div class='card'>
            <div class='card-header'>
                <h3>Passenger Information</h3>
            </div>
            <div class='card-body'>
                <p><strong>Name:</strong> {user.UserName}</p>
                <p><strong>EGN:</strong> {user.EGN}</p>
                <p><strong>Phone:</strong> {user.PhoneNumber}</p>
                <p><strong>Nationality:</strong> {reservation.Nationality}</p>
                <p><strong>Ticket Type:</strong> {reservation.TicketType}</p>
            </div>
        </div>
        
        <div class='card'>
            <div class='card-header'>
                <h3>Flight Information</h3>
            </div>
            <div class='card-body'>
                <p><strong>Flight Number:</strong> {flight.AircraftNumber}</p>
                <p><strong>Route:</strong> {flight.FromLocation} → {flight.ToLocation}</p>
                <p><strong>Departure:</strong> {flight.DepartureTime.ToString("g")}</p>
                <p><strong>Arrival:</strong> {flight.ArrivalTime.ToString("g")}</p>
                <p><strong>Duration:</strong> {duration.ToString(@"hh\:mm")}</p>
                <p><strong>Aircraft Type:</strong> {flight.AircraftType}</p>
            </div>
        </div>
        
        <div class='reservation-id'>
            <strong>Reservation ID:</strong> {reservation.ReservationUserId}
        </div>
        
        <p>Please keep this reservation ID safe as you'll need it to manage your booking.</p>
    </div>
    
    <div class='footer'>
        <p>Thank you for choosing our service!</p>
        <p>If you have any questions, please contact our customer support.</p>
    </div>
</body>
</html>
";
        }
    }
}
