using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ReservArte.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfirmationTokens",
                columns: table => new
                {
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfirmationTokens", x => x.Token);
                    table.CheckConstraint("CK_ConfirmationTokens_Action", "\"Action\" IN ('confirm', 'cancel')");
                    table.ForeignKey(
                        name: "FK_ConfirmationTokens_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfirmationTokens_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "es"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplates", x => x.Id);
                    table.CheckConstraint("CK_MessageTemplates_Type", "\"Type\" IN ('email_reminder', 'whatsapp_reminder', 'confirmation')");
                    table.ForeignKey(
                        name: "FK_MessageTemplates_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReminderConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReminderOrder = table.Column<int>(type: "integer", nullable: false),
                    HoursBeforeAppointment = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MessageTemplateId = table.Column<int>(type: "integer", nullable: false),
                    AllowedSendStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    AllowedSendEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderConfigurations", x => x.Id);
                    table.CheckConstraint("CK_ReminderConfigurations_Channel", "\"Channel\" IN ('email', 'whatsapp', 'both')");
                    table.CheckConstraint("CK_ReminderConfigurations_HoursBeforeAppointment", "\"HoursBeforeAppointment\" >= 1");
                    table.CheckConstraint("CK_ReminderConfigurations_ReminderOrder", "\"ReminderOrder\" >= 1");
                    table.CheckConstraint("CK_ReminderConfigurations_SendWindow", "(\"AllowedSendStartTime\" IS NULL AND \"AllowedSendEndTime\" IS NULL) OR (\"AllowedSendStartTime\" IS NOT NULL AND \"AllowedSendEndTime\" IS NOT NULL AND \"AllowedSendEndTime\" > \"AllowedSendStartTime\")");
                    table.ForeignKey(
                        name: "FK_ReminderConfigurations_MessageTemplates_MessageTemplateId",
                        column: x => x.MessageTemplateId,
                        principalTable: "MessageTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReminderConfigurations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReminderLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<int>(type: "integer", nullable: false),
                    ReminderConfigurationId = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExternalMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderLogs", x => x.Id);
                    table.CheckConstraint("CK_ReminderLogs_Channel", "\"Channel\" IN ('email', 'whatsapp')");
                    table.CheckConstraint("CK_ReminderLogs_Status", "\"Status\" IN ('pending', 'sent', 'failed', 'delivered', 'opened')");
                    table.ForeignKey(
                        name: "FK_ReminderLogs_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReminderLogs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReminderLogs_ReminderConfigurations_ReminderConfigurationId",
                        column: x => x.ReminderConfigurationId,
                        principalTable: "ReminderConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmationTokens_AppointmentId",
                table: "ConfirmationTokens",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmationTokens_OrganizationId",
                table: "ConfirmationTokens",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_OrganizationId_Name",
                table: "MessageTemplates",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderConfigurations_MessageTemplateId",
                table: "ReminderConfigurations",
                column: "MessageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderConfigurations_OrganizationId_ReminderOrder",
                table: "ReminderConfigurations",
                columns: new[] { "OrganizationId", "ReminderOrder" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderLogs_AppointmentId_ReminderConfigurationId",
                table: "ReminderLogs",
                columns: new[] { "AppointmentId", "ReminderConfigurationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderLogs_ExternalMessageId",
                table: "ReminderLogs",
                column: "ExternalMessageId",
                filter: "\"ExternalMessageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderLogs_Organization_Appointment_Configuration_Channel",
                table: "ReminderLogs",
                columns: new[] { "OrganizationId", "AppointmentId", "ReminderConfigurationId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReminderLogs_ReminderConfigurationId",
                table: "ReminderLogs",
                column: "ReminderConfigurationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfirmationTokens");

            migrationBuilder.DropTable(
                name: "ReminderLogs");

            migrationBuilder.DropTable(
                name: "ReminderConfigurations");

            migrationBuilder.DropTable(
                name: "MessageTemplates");
        }
    }
}
