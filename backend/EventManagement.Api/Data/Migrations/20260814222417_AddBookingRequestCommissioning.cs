using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingRequestCommissioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BudgetMaximumMinor",
                table: "BookingRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BudgetMinimumMinor",
                table: "BookingRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventCategory",
                table: "BookingRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpectedEndDate",
                table: "BookingRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceLinks",
                table: "BookingRequests",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresRegistration",
                table: "BookingRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresTicketing",
                table: "BookingRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresVoting",
                table: "BookingRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TrackingTokenHash",
                table: "BookingRequests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingRequestQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedFeeMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ProposedTimeline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRequestQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingRequestQuotes_BookingRequests_BookingRequestId",
                        column: x => x.BookingRequestId,
                        principalTable: "BookingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingRequestQuotes_Users_OrganizerId",
                        column: x => x.OrganizerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingRequestStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRequestStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingRequestStatusHistory_BookingRequests_BookingRequestId",
                        column: x => x.BookingRequestId,
                        principalTable: "BookingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_TrackingTokenHash",
                table: "BookingRequests",
                column: "TrackingTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequestQuotes_BookingRequestId",
                table: "BookingRequestQuotes",
                column: "BookingRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequestQuotes_OrganizerId",
                table: "BookingRequestQuotes",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequestStatusHistory_BookingRequestId_CreatedAt",
                table: "BookingRequestStatusHistory",
                columns: new[] { "BookingRequestId", "CreatedAt" });

            migrationBuilder.Sql("""
                INSERT INTO "BookingRequestStatusHistory" ("Id", "BookingRequestId", "Status", "Note", "CreatedAt")
                SELECT gen_random_uuid(), "Id", "Status", 'Status at commissioning migration.', "UpdatedAt"
                FROM "BookingRequests";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingRequestQuotes");

            migrationBuilder.DropTable(
                name: "BookingRequestStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_TrackingTokenHash",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "BudgetMaximumMinor",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "BudgetMinimumMinor",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "EventCategory",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "ExpectedEndDate",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "ReferenceLinks",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "RequiresRegistration",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "RequiresTicketing",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "RequiresVoting",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "TrackingTokenHash",
                table: "BookingRequests");
        }
    }
}
