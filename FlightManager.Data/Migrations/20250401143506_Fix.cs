#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace FlightManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_ReservationUsers_ReservationUserId",
                table: "Reservations");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_ReservationUsers_ReservationUserId",
                table: "Reservations",
                column: "ReservationUserId",
                principalTable: "ReservationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_ReservationUsers_ReservationUserId",
                table: "Reservations");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_ReservationUsers_ReservationUserId",
                table: "Reservations",
                column: "ReservationUserId",
                principalTable: "ReservationUsers",
                principalColumn: "Id");
        }
    }
}
