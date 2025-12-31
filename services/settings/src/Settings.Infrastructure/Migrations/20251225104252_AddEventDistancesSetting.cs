using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Settings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventDistancesSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.email.body_template",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.email.subject_template",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.reminder.offsets_minutes",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.smtp.from",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.smtp.host",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.smtp.port",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "planning.default_timezone",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "planning.max_plans_per_user",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "SettingDefinitions",
                columns: new[] { "Key", "CreatedAt", "Description", "IsRequired", "IsSensitive", "JsonSchema", "UpdatedAt", "ValueType" },
                values: new object[,]
                {
                    { "audit.default_page_size", new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Default page size for audit log queries", false, false, null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Integer" },
                    { "event.distances", new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Available event distances (km, label, displayOrder)", false, false, null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Json" },
                    { "gateway.http_client.timeout_seconds", new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Gateway HTTP client timeout in seconds", false, false, null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Integer" },
                    { "notifications.dispatcher.batch_size", new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Batch size for notification dispatcher", false, false, null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Integer" },
                    { "notifications.dispatcher.max_attempts", new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Max retry attempts for notifications", false, false, null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Integer" },
                    { "notifications.dispatcher.retry_backoff_base_seconds", new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Base seconds for exponential retry backoff", false, false, null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Integer" },
                    { "platform.invite.expiration_days", new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Invite expiration in days", false, false, null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Integer" },
                    { "platform.membership.cache_ttl_minutes", new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Membership validation cache TTL in minutes", false, false, null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "Integer" }
                });

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000001"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000002"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000003"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000004"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000005"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000006"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000007"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000008"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.InsertData(
                table: "SettingValues",
                columns: new[] { "Id", "Environment", "Key", "TenantId", "UpdatedAt", "ValueJson", "Version" },
                values: new object[,]
                {
                    { new Guid("11111111-0001-0001-0001-000000000009"), "dev", "notifications.dispatcher.max_attempts", null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "3", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000010"), "dev", "notifications.dispatcher.batch_size", null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "50", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000011"), "dev", "notifications.dispatcher.retry_backoff_base_seconds", null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "10", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000012"), "dev", "platform.invite.expiration_days", null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "7", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000013"), "dev", "platform.membership.cache_ttl_minutes", null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "5", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000014"), "dev", "gateway.http_client.timeout_seconds", null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "5", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000015"), "dev", "audit.default_page_size", null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "50", 1L },
                    { new Guid("11111111-0001-0001-0001-000000000016"), "dev", "event.distances", null, new DateTimeOffset(new DateTime(2025, 12, 25, 10, 42, 51, 839, DateTimeKind.Unspecified).AddTicks(180), new TimeSpan(0, 0, 0, 0, 0)), "[{\"km\":5,\"label\":\"5K\",\"displayOrder\":1},{\"km\":10,\"label\":\"10K\",\"displayOrder\":2},{\"km\":21,\"label\":\"21K\",\"displayOrder\":3},{\"km\":42,\"label\":\"42K\",\"displayOrder\":4},{\"km\":100,\"label\":\"ultra\",\"displayOrder\":5}]", 1L }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000009"));

            migrationBuilder.DeleteData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000010"));

            migrationBuilder.DeleteData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000011"));

            migrationBuilder.DeleteData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000012"));

            migrationBuilder.DeleteData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000013"));

            migrationBuilder.DeleteData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000014"));

            migrationBuilder.DeleteData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000015"));

            migrationBuilder.DeleteData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000016"));

            migrationBuilder.DeleteData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "audit.default_page_size");

            migrationBuilder.DeleteData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "event.distances");

            migrationBuilder.DeleteData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "gateway.http_client.timeout_seconds");

            migrationBuilder.DeleteData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.dispatcher.batch_size");

            migrationBuilder.DeleteData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.dispatcher.max_attempts");

            migrationBuilder.DeleteData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.dispatcher.retry_backoff_base_seconds");

            migrationBuilder.DeleteData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "platform.invite.expiration_days");

            migrationBuilder.DeleteData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "platform.membership.cache_ttl_minutes");

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.email.body_template",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.email.subject_template",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.reminder.offsets_minutes",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.smtp.from",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.smtp.host",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "notifications.smtp.port",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "planning.default_timezone",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingDefinitions",
                keyColumn: "Key",
                keyValue: "planning.max_plans_per_user",
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000001"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000002"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000003"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000004"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000005"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000006"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000007"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SettingValues",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000008"),
                column: "UpdatedAt",
                value: new DateTimeOffset(new DateTime(2025, 12, 23, 10, 33, 1, 455, DateTimeKind.Unspecified).AddTicks(3160), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
