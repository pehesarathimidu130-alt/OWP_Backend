using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRangeFieldsToStrings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VideographerCount",
                table: "PhotographyDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhotographerCount",
                table: "PhotographyDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WirelessMics",
                table: "MusicDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ParkingCapacity",
                table: "HotelVenueDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OutdoorCeremonyCapacity",
                table: "HotelVenueDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NumberOfGuestRooms",
                table: "HotelVenueDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 9, 6, 57, 912, DateTimeKind.Utc).AddTicks(8610), new DateTime(2026, 9, 16, 9, 6, 57, 912, DateTimeKind.Utc).AddTicks(8614) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 9, 6, 57, 912, DateTimeKind.Utc).AddTicks(8617), new DateTime(2026, 9, 16, 9, 6, 57, 912, DateTimeKind.Utc).AddTicks(8618) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 9, 6, 57, 912, DateTimeKind.Utc).AddTicks(8621), new DateTime(2026, 9, 16, 9, 6, 57, 912, DateTimeKind.Utc).AddTicks(8622) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 9, 6, 57, 912, DateTimeKind.Utc).AddTicks(8624), new DateTime(2026, 9, 16, 9, 6, 57, 912, DateTimeKind.Utc).AddTicks(8625) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 9, 6, 57, 912, DateTimeKind.Utc).AddTicks(8627), new DateTime(2026, 9, 16, 9, 6, 57, 912, DateTimeKind.Utc).AddTicks(8628) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "VideographerCount",
                table: "PhotographyDetails",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PhotographerCount",
                table: "PhotographyDetails",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WirelessMics",
                table: "MusicDetails",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ParkingCapacity",
                table: "HotelVenueDetails",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OutdoorCeremonyCapacity",
                table: "HotelVenueDetails",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "NumberOfGuestRooms",
                table: "HotelVenueDetails",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3665), new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3668) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3669), new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3669) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3670), new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3671) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3671), new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3672) });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3672), new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3673) });
        }
    }
}
