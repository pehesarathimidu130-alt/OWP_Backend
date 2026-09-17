using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class RedesignVendorListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Category",
                table: "VendorServices",
                newName: "Status");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VendorServices",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Draft",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "VendorServices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPriceOnRequest",
                table: "VendorServices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "VendorServices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "CateringDetails",
                columns: table => new
                {
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    ServiceStyle = table.Column<string>(type: "text", nullable: true),
                    Cuisines = table.Column<string[]>(type: "text[]", nullable: true),
                    DietaryOptions = table.Column<string[]>(type: "text[]", nullable: true),
                    MinGuests = table.Column<int>(type: "integer", nullable: true),
                    MaxGuests = table.Column<int>(type: "integer", nullable: true),
                    PricePerHead = table.Column<decimal>(type: "numeric", nullable: true),
                    WaitstaffIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    GlasswareIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    CrockeryCutlery = table.Column<bool>(type: "boolean", nullable: true),
                    ChafingDishesIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    FurnitureRentalAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    SetupTeardownIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    OutstationCatering = table.Column<bool>(type: "boolean", nullable: true),
                    KitchenRequirement = table.Column<string>(type: "text", nullable: true),
                    TastingAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    TastingPolicy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CateringDetails", x => x.ServiceId);
                    table.ForeignKey(
                        name: "FK_CateringDetails_VendorServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "VendorServices",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DecorationsDetails",
                columns: table => new
                {
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    PrimaryStyles = table.Column<string[]>(type: "text[]", nullable: true),
                    ProvidesFlorals = table.Column<bool>(type: "boolean", nullable: false),
                    FloralTypes = table.Column<string[]>(type: "text[]", nullable: true),
                    AvailableSetups = table.Column<string[]>(type: "text[]", nullable: true),
                    TablewareLinens = table.Column<bool>(type: "boolean", nullable: true),
                    CustomSignageIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    LoungePropsAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    SetupTimeRequired = table.Column<string>(type: "text", nullable: true),
                    SameDayTeardownIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    VenueRestrictions = table.Column<string>(type: "text", nullable: true),
                    OutstationDecorAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    TravelFeePolicy = table.Column<string>(type: "text", nullable: true),
                    FreeConsultation = table.Column<bool>(type: "boolean", nullable: true),
                    CustomMoodboards = table.Column<bool>(type: "boolean", nullable: true),
                    DesignFeePolicy = table.Column<string>(type: "text", nullable: true),
                    MinimumBudget = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecorationsDetails", x => x.ServiceId);
                    table.ForeignKey(
                        name: "FK_DecorationsDetails_VendorServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "VendorServices",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HotelVenueDetails",
                columns: table => new
                {
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    VenueType = table.Column<string>(type: "text", nullable: true),
                    VenueSetting = table.Column<string>(type: "text", nullable: true),
                    IndoorOutdoor = table.Column<string>(type: "text", nullable: true),
                    ParkingCapacity = table.Column<int>(type: "integer", nullable: true),
                    ParkingType = table.Column<string>(type: "text", nullable: true),
                    ValetParking = table.Column<bool>(type: "boolean", nullable: true),
                    Wifi = table.Column<bool>(type: "boolean", nullable: true),
                    WheelchairAccessible = table.Column<bool>(type: "boolean", nullable: true),
                    HasAirConditioning = table.Column<bool>(type: "boolean", nullable: true),
                    HasBackupGenerator = table.Column<bool>(type: "boolean", nullable: true),
                    HasElevator = table.Column<bool>(type: "boolean", nullable: true),
                    HasGuestDropOff = table.Column<bool>(type: "boolean", nullable: true),
                    HasVendorLoadingAccess = table.Column<bool>(type: "boolean", nullable: true),
                    HasCeremony = table.Column<bool>(type: "boolean", nullable: false),
                    CeremonyLocation = table.Column<string>(type: "text", nullable: true),
                    OutdoorCeremonyCapacity = table.Column<int>(type: "integer", nullable: true),
                    SeparateCeremonyReceptionSpaces = table.Column<bool>(type: "boolean", nullable: true),
                    HasCatering = table.Column<bool>(type: "boolean", nullable: false),
                    CateringProvidedBy = table.Column<string>(type: "text", nullable: true),
                    OutsideFoodAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    KitchenFacility = table.Column<string>(type: "text", nullable: true),
                    CuisineOptions = table.Column<string[]>(type: "text[]", nullable: true),
                    BuffetAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    PlatedDinnerAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    CustomMenuAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    CakeCuttingAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    HasBeverages = table.Column<bool>(type: "boolean", nullable: false),
                    BeverageService = table.Column<string>(type: "text", nullable: true),
                    BarFacility = table.Column<string>(type: "text", nullable: true),
                    OutsideBeveragesAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    HasAccommodation = table.Column<bool>(type: "boolean", nullable: false),
                    NumberOfGuestRooms = table.Column<int>(type: "integer", nullable: true),
                    ComplimentaryBridalSuite = table.Column<bool>(type: "boolean", nullable: true),
                    RoomTypes = table.Column<string[]>(type: "text[]", nullable: true),
                    BridalSuiteAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    GuestAccommodationAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    OnSiteAccommodation = table.Column<bool>(type: "boolean", nullable: true),
                    HasEntertainment = table.Column<bool>(type: "boolean", nullable: false),
                    DjAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    LiveBandAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    MaxMusicEndTime = table.Column<string>(type: "text", nullable: true),
                    ProjectorScreen = table.Column<bool>(type: "boolean", nullable: true),
                    TraditionalMusicAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    HasDecoration = table.Column<bool>(type: "boolean", nullable: false),
                    DecorationPolicy = table.Column<string>(type: "text", nullable: true),
                    TableDecoration = table.Column<bool>(type: "boolean", nullable: true),
                    LightingDecoration = table.Column<bool>(type: "boolean", nullable: true),
                    OutsideDecoratorAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    BasicDecorationIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    FloralDecorationAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    StageDecorationAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    HasPhotographyPolicy = table.Column<bool>(type: "boolean", nullable: false),
                    PhotographyAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    ExternalPhotographerAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    PreWeddingShootAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    PhotographyLocations = table.Column<string[]>(type: "text[]", nullable: true),
                    HasPolicies = table.Column<bool>(type: "boolean", nullable: false),
                    DepositRequired = table.Column<bool>(type: "boolean", nullable: true),
                    DepositAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    MinimumGuestCount = table.Column<int>(type: "integer", nullable: true),
                    MinimumBookingDuration = table.Column<string>(type: "text", nullable: true),
                    CancellationPolicy = table.Column<string>(type: "text", nullable: true),
                    OutsideVendorRestrictions = table.Column<string>(type: "text", nullable: true),
                    AdditionalCharges = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelVenueDetails", x => x.ServiceId);
                    table.ForeignKey(
                        name: "FK_HotelVenueDetails_VendorServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "VendorServices",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicDetails",
                columns: table => new
                {
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    PerformanceType = table.Column<string>(type: "text", nullable: true),
                    LineupSize = table.Column<string>(type: "text", nullable: true),
                    SetDuration = table.Column<string>(type: "text", nullable: true),
                    Genres = table.Column<string[]>(type: "text[]", nullable: true),
                    SoundSystemIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    SoundSystemCapacity = table.Column<string>(type: "text", nullable: true),
                    WirelessMics = table.Column<int>(type: "integer", nullable: true),
                    StageLightingIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    LightingRig = table.Column<string>(type: "text", nullable: true),
                    SetupTimeRequired = table.Column<string>(type: "text", nullable: true),
                    BackupHardwareOnSite = table.Column<bool>(type: "boolean", nullable: true),
                    McServicesIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    BreakMusicIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    CustomSongsAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    OvertimeRate = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicDetails", x => x.ServiceId);
                    table.ForeignKey(
                        name: "FK_MusicDetails_VendorServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "VendorServices",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhotographyDetails",
                columns: table => new
                {
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    ShootingStyle = table.Column<string>(type: "text", nullable: true),
                    HoursOfCoverage = table.Column<string>(type: "text", nullable: true),
                    IncludedServices = table.Column<string[]>(type: "text[]", nullable: true),
                    PhotosDelivered = table.Column<string>(type: "text", nullable: true),
                    DeliveryTimeframe = table.Column<string>(type: "text", nullable: true),
                    RawFilesIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    DigitalGalleryIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    AlbumIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    AlbumType = table.Column<string>(type: "text", nullable: true),
                    AlbumPages = table.Column<string>(type: "text", nullable: true),
                    PhotographerCount = table.Column<int>(type: "integer", nullable: true),
                    DroneAllowed = table.Column<bool>(type: "boolean", nullable: true),
                    BackupGear = table.Column<bool>(type: "boolean", nullable: true),
                    VideographyIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    VideographerCount = table.Column<int>(type: "integer", nullable: true),
                    VideoLength = table.Column<string>(type: "text", nullable: true),
                    VideoDeliverables = table.Column<string[]>(type: "text[]", nullable: true),
                    TravelOutsideColombo = table.Column<bool>(type: "boolean", nullable: false),
                    OutstationAccommodationRequired = table.Column<bool>(type: "boolean", nullable: true),
                    DepositRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    CancellationPolicy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotographyDetails", x => x.ServiceId);
                    table.ForeignKey(
                        name: "FK_PhotographyDetails_VendorServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "VendorServices",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenueSpaces",
                columns: table => new
                {
                    VenueSpaceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    SpaceName = table.Column<string>(type: "text", nullable: false),
                    SpaceType = table.Column<string>(type: "text", nullable: false),
                    SeatedCapacity = table.Column<int>(type: "integer", nullable: true),
                    FloatingCapacity = table.Column<int>(type: "integer", nullable: true),
                    AirConditioned = table.Column<bool>(type: "boolean", nullable: true),
                    KeyFeatures = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueSpaces", x => x.VenueSpaceId);
                    table.ForeignKey(
                        name: "FK_VenueSpaces_VendorServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "VendorServices",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CategoryName", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Hotel / Venue", new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3665), new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3668) },
                    { 2, "Photography", new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3669), new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3669) },
                    { 3, "Music", new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3670), new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3671) },
                    { 4, "Decorations", new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3671), new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3672) },
                    { 5, "Catering", new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3672), new DateTime(2026, 9, 16, 8, 13, 2, 289, DateTimeKind.Utc).AddTicks(3673) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorServices_CategoryId",
                table: "VendorServices",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueSpaces_ServiceId",
                table: "VenueSpaces",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorServices_Categories_CategoryId",
                table: "VendorServices",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorServices_Categories_CategoryId",
                table: "VendorServices");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "CateringDetails");

            migrationBuilder.DropTable(
                name: "DecorationsDetails");

            migrationBuilder.DropTable(
                name: "HotelVenueDetails");

            migrationBuilder.DropTable(
                name: "MusicDetails");

            migrationBuilder.DropTable(
                name: "PhotographyDetails");

            migrationBuilder.DropTable(
                name: "VenueSpaces");

            migrationBuilder.DropIndex(
                name: "IX_VendorServices_CategoryId",
                table: "VendorServices");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "VendorServices");

            migrationBuilder.DropColumn(
                name: "IsPriceOnRequest",
                table: "VendorServices");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "VendorServices");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VendorServices",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Draft");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "VendorServices",
                newName: "Category");
        }
    }
}
