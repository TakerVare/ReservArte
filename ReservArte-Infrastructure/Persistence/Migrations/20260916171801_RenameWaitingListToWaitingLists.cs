using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservArte.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameWaitingListToWaitingLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WaitingList_Customers_CustomerId",
                table: "WaitingList");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitingList_Employees_PreferredEmployeeId",
                table: "WaitingList");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitingList_Organizations_OrganizationId",
                table: "WaitingList");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitingList_Services_ServiceId",
                table: "WaitingList");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WaitingList",
                table: "WaitingList");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WaitingList_DateRange",
                table: "WaitingList");

            migrationBuilder.RenameTable(
                name: "WaitingList",
                newName: "WaitingLists");

            migrationBuilder.RenameIndex(
                name: "IX_WaitingList_ServiceId",
                table: "WaitingLists",
                newName: "IX_WaitingLists_ServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_WaitingList_PreferredEmployeeId",
                table: "WaitingLists",
                newName: "IX_WaitingLists_PreferredEmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_WaitingList_CustomerId",
                table: "WaitingLists",
                newName: "IX_WaitingLists_CustomerId");

            migrationBuilder.RenameIndex(
                name: "idx_waiting_list_org_service_priority",
                table: "WaitingLists",
                newName: "idx_waiting_lists_org_service_priority");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WaitingLists",
                table: "WaitingLists",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WaitingLists_DateRange",
                table: "WaitingLists",
                sql: "[DateRangeEnd] > [DateRangeStart]");

            migrationBuilder.AddForeignKey(
                name: "FK_WaitingLists_Customers_CustomerId",
                table: "WaitingLists",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitingLists_Employees_PreferredEmployeeId",
                table: "WaitingLists",
                column: "PreferredEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitingLists_Organizations_OrganizationId",
                table: "WaitingLists",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitingLists_Services_ServiceId",
                table: "WaitingLists",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WaitingLists_Customers_CustomerId",
                table: "WaitingLists");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitingLists_Employees_PreferredEmployeeId",
                table: "WaitingLists");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitingLists_Organizations_OrganizationId",
                table: "WaitingLists");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitingLists_Services_ServiceId",
                table: "WaitingLists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WaitingLists",
                table: "WaitingLists");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WaitingLists_DateRange",
                table: "WaitingLists");

            migrationBuilder.RenameTable(
                name: "WaitingLists",
                newName: "WaitingList");

            migrationBuilder.RenameIndex(
                name: "IX_WaitingLists_ServiceId",
                table: "WaitingList",
                newName: "IX_WaitingList_ServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_WaitingLists_PreferredEmployeeId",
                table: "WaitingList",
                newName: "IX_WaitingList_PreferredEmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_WaitingLists_CustomerId",
                table: "WaitingList",
                newName: "IX_WaitingList_CustomerId");

            migrationBuilder.RenameIndex(
                name: "idx_waiting_lists_org_service_priority",
                table: "WaitingList",
                newName: "idx_waiting_list_org_service_priority");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WaitingList",
                table: "WaitingList",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WaitingList_DateRange",
                table: "WaitingList",
                sql: "[DateRangeEnd] > [DateRangeStart]");

            migrationBuilder.AddForeignKey(
                name: "FK_WaitingList_Customers_CustomerId",
                table: "WaitingList",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitingList_Employees_PreferredEmployeeId",
                table: "WaitingList",
                column: "PreferredEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitingList_Organizations_OrganizationId",
                table: "WaitingList",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitingList_Services_ServiceId",
                table: "WaitingList",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
