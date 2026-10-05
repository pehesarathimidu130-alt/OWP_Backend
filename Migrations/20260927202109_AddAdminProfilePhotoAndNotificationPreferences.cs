using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminProfilePhotoAndNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyAiWorkflowApproval",
                table: "Admins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyCustomerComplaint",
                table: "Admins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyFlaggedContent",
                table: "Admins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyNewVendorPending",
                table: "Admins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyWeeklySummary",
                table: "Admins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "Admins",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyAiWorkflowApproval",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "NotifyCustomerComplaint",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "NotifyFlaggedContent",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "NotifyNewVendorPending",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "NotifyWeeklySummary",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "Admins");
        }
    }
}
