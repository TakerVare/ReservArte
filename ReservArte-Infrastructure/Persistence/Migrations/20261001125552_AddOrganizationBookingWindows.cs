using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservArte.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationBookingWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerBookingWindowWeeks",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddColumn<int>(
                name: "StaffBookingWindowWeeks",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Organizations_CustomerBookingWindowWeeks",
                table: "Organizations",
                sql: "\"CustomerBookingWindowWeeks\" BETWEEN 1 AND 52");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Organizations_StaffBookingWindowWeeks",
                table: "Organizations",
                sql: "\"StaffBookingWindowWeeks\" BETWEEN 1 AND 52");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Organizations_CustomerBookingWindowWeeks",
                table: "Organizations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Organizations_StaffBookingWindowWeeks",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CustomerBookingWindowWeeks",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StaffBookingWindowWeeks",
                table: "Organizations");
        }
    }
}
