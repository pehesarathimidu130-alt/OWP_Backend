using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationFieldsToVendorServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""VendorServices"" 
                ADD COLUMN IF NOT EXISTS ""Latitude"" double precision,
                ADD COLUMN IF NOT EXISTS ""Longitude"" double precision,
                ADD COLUMN IF NOT EXISTS ""LocationAddress"" character varying(300),
                ADD COLUMN IF NOT EXISTS ""GooglePlaceId"" character varying(100),
                ADD COLUMN IF NOT EXISTS ""ServiceRadiusKm"" double precision;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""VendorServices"" 
                DROP COLUMN IF EXISTS ""Latitude"",
                DROP COLUMN IF EXISTS ""Longitude"",
                DROP COLUMN IF EXISTS ""LocationAddress"",
                DROP COLUMN IF EXISTS ""GooglePlaceId"",
                DROP COLUMN IF EXISTS ""ServiceRadiusKm"";
            ");
        }
    }
}
