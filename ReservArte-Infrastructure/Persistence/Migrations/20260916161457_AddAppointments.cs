using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservArte.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AppointmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    RedsysOrderNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RedsysPreAuthToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledById = table.Column<int>(type: "int", nullable: true),
                    CancelledByType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.CheckConstraint("CK_Appointments_Amounts", "[TotalPrice] >= 0 AND [DepositAmount] >= 0");
                    table.CheckConstraint("CK_Appointments_CancelledByType", "[CancelledByType] IN ('customer', 'business')");
                    table.CheckConstraint("CK_Appointments_EndTime", "[EndTime] > [StartTime]");
                    table.CheckConstraint("CK_Appointments_Status", "[Status] IN ('pending', 'confirmed', 'in_progress', 'completed', 'cancelled', 'cancelled_by_customer', 'cancelled_by_business', 'no_show')");
                    table.ForeignKey(
                        name: "FK_Appointments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WaitingList",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    PreferredEmployeeId = table.Column<int>(type: "int", nullable: true),
                    PreferredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateRangeStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateRangeEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitingList", x => x.Id);
                    table.CheckConstraint("CK_WaitingList_DateRange", "[DateRangeEnd] > [DateRangeStart]");
                    table.ForeignKey(
                        name: "FK_WaitingList_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WaitingList_Employees_PreferredEmployeeId",
                        column: x => x.PreferredEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitingList_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitingList_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentServiceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    ServiceVariationId = table.Column<int>(type: "int", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentServiceItems", x => x.Id);
                    table.CheckConstraint("CK_AppointmentServiceItems_PriceAndDuration", "[Price] >= 0 AND [DurationMinutes] > 0");
                    table.ForeignKey(
                        name: "FK_AppointmentServiceItems_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentServiceItems_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentServiceItems_ServiceVariations_ServiceVariationId",
                        column: x => x.ServiceVariationId,
                        principalTable: "ServiceVariations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentServiceItems_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_appointments_org_date",
                table: "Appointments",
                columns: new[] { "OrganizationId", "AppointmentDate" });

            migrationBuilder.CreateIndex(
                name: "idx_appointments_redsys_order",
                table: "Appointments",
                column: "RedsysOrderNumber",
                unique: true,
                filter: "[RedsysOrderNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CustomerId",
                table: "Appointments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_EmployeeId_AppointmentDate",
                table: "Appointments",
                columns: new[] { "EmployeeId", "AppointmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentServiceItems_AppointmentId_Order",
                table: "AppointmentServiceItems",
                columns: new[] { "AppointmentId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentServiceItems_OrganizationId",
                table: "AppointmentServiceItems",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentServiceItems_ServiceId",
                table: "AppointmentServiceItems",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentServiceItems_ServiceVariationId",
                table: "AppointmentServiceItems",
                column: "ServiceVariationId");

            migrationBuilder.CreateIndex(
                name: "idx_waiting_list_org_service_priority",
                table: "WaitingList",
                columns: new[] { "OrganizationId", "ServiceId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitingList_CustomerId",
                table: "WaitingList",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitingList_PreferredEmployeeId",
                table: "WaitingList",
                column: "PreferredEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitingList_ServiceId",
                table: "WaitingList",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentServiceItems");

            migrationBuilder.DropTable(
                name: "WaitingList");

            migrationBuilder.DropTable(
                name: "Appointments");
        }
    }
}
