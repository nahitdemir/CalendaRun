using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Settings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SettingAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OldValueJson = table.Column<string>(type: "text", nullable: true),
                    NewValueJson = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettingDefinitions",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ValueType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    JsonSchema = table.Column<string>(type: "text", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsSensitive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingDefinitions", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "SettingValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Environment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValueJson = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettingValues_SettingDefinitions_Key",
                        column: x => x.Key,
                        principalTable: "SettingDefinitions",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SettingDefinitions",
                columns: new[] { "Key", "CreatedAt", "Description", "IsRequired", "IsSensitive", "JsonSchema", "UpdatedAt", "ValueType" },
                values: new object[,]
                {
                    { "notifications.email.body_template", new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "Email Body Template", false, false, null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "String" },
                    { "notifications.email.subject_template", new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "Email Subject Template", false, false, null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "String" },
                    { "notifications.reminder.offsets_minutes", new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "Reminder offsets in minutes", false, false, null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "StringArray" },
                    { "notifications.smtp.from", new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "Default From Email", true, false, null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "String" },
                    { "notifications.smtp.host", new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "SMTP Host", true, false, null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "String" },
                    { "notifications.smtp.port", new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "SMTP Port", true, false, null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "Integer" },
                    { "planning.default_timezone", new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "Default Timezone", true, false, null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "String" },
                    { "planning.max_plans_per_user", new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "Max plans per user", false, false, null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "Integer" }
                });

            migrationBuilder.InsertData(
                table: "SettingValues",
                columns: new[] { "Id", "Environment", "Key", "TenantId", "UpdatedAt", "ValueJson", "Version" },
                values: new object[,]
                {
                    { new Guid("11111111-0001-0001-0001-000000000001"), "dev", "notifications.smtp.host", null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "\"localhost\"", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000002"), "dev", "notifications.smtp.port", null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "1025", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000003"), "dev", "notifications.smtp.from", null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "\"noreply@calendarun.local\"", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000004"), "dev", "notifications.email.subject_template", null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "\"You planned event: {EventId}\"", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000005"), "dev", "notifications.email.body_template", null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "\"Hello!\\n\\nYou have planned event {EventId}.\\nPlan ID: {PlanItemId}\\n\\nBest regards,\\nCalendaRun Team\"", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000006"), "dev", "notifications.reminder.offsets_minutes", null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "[1440, 60, 15]", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000007"), "dev", "planning.default_timezone", null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "\"Europe/Istanbul\"", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000008"), "dev", "planning.max_plans_per_user", null, new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), "100", 1L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SettingAudits_UpdatedAt",
                table: "SettingAudits",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SettingValues_Key_TenantId_Environment",
                table: "SettingValues",
                columns: new[] { "Key", "TenantId", "Environment" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SettingAudits");

            migrationBuilder.DropTable(
                name: "SettingValues");

            migrationBuilder.DropTable(
                name: "SettingDefinitions");
        }
    }
}
