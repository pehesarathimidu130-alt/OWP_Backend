using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class RepairAdminSecurePinColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Admins\" ADD COLUMN IF NOT EXISTS \"SecurePin\" character varying(4) NOT NULL DEFAULT '9999';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Admins\" DROP COLUMN IF EXISTS \"SecurePin\";");
        }
    }
}
