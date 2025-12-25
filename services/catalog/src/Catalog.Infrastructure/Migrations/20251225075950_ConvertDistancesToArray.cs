using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertDistancesToArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert string column to integer array using USING clause
            migrationBuilder.Sql(@"
                ALTER TABLE ""Events""
                ALTER COLUMN ""Distances"" TYPE integer[]
                USING CASE
                    WHEN ""Distances"" IS NULL OR ""Distances"" = '' THEN NULL
                    ELSE string_to_array(""Distances"", ',')::integer[]
                END;
            ");

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "Distances",
                value: new[] { 10, 21, 42 });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "Distances",
                value: new[] { 100 });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "Distances",
                value: new[] { 5, 10, 21 });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Distances",
                table: "Events",
                column: "Distances")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_Distances",
                table: "Events");

            // Convert integer array back to string using array_to_string
            migrationBuilder.Sql(@"
                ALTER TABLE ""Events""
                ALTER COLUMN ""Distances"" TYPE character varying(100)
                USING CASE
                    WHEN ""Distances"" IS NULL THEN NULL
                    ELSE array_to_string(""Distances"", ',')
                END;
            ");

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "Distances",
                value: "10,21,42");

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "Distances",
                value: "100");

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "Distances",
                value: "5,10,21");
        }
    }
}
