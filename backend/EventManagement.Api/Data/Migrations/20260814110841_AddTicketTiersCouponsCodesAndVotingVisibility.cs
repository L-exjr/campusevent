using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTiersCouponsCodesAndVotingVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowLiveResults",
                table: "VotingCampaigns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CouponId",
                table: "PaymentOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DiscountAmountMinor",
                table: "PaymentOrders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OriginalAmountMinor",
                table: "PaymentOrders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "TicketTierId",
                table: "PaymentOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketCode",
                table: "EventRegistrations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PercentageDiscount = table.Column<int>(type: "integer", nullable: false),
                    UsageLimit = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coupons_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Coupons_Users_OrganizerId",
                        column: x => x.OrganizerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketTiers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Every existing single-price event becomes one equivalent General tier.
            // Reusing the event UUID makes the backfill deterministic and lets all
            // historical payment orders be linked without guessing.
            migrationBuilder.Sql(
                """
                INSERT INTO "TicketTiers" ("Id", "EventId", "Name", "PriceMinor", "Capacity", "Position", "IsActive")
                SELECT "Id", "Id", 'General', "PriceMinor", "Capacity", 0, TRUE
                FROM "Events";

                UPDATE "PaymentOrders"
                SET "TicketTierId" = "EventId",
                    "OriginalAmountMinor" = "AmountMinor",
                    "DiscountAmountMinor" = 0;

                UPDATE "EventRegistrations"
                SET "TicketCode" = 'EMS-' || UPPER(SUBSTRING(REPLACE("Id"::text, '-', '') FROM 1 FOR 16));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_CouponId",
                table: "PaymentOrders",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_TicketTierId",
                table: "PaymentOrders",
                column: "TicketTierId");

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_TicketCode",
                table: "EventRegistrations",
                column: "TicketCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                table: "Coupons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_EventId",
                table: "Coupons",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_OrganizerId_IsActive",
                table: "Coupons",
                columns: new[] { "OrganizerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketTiers_EventId_Name",
                table: "TicketTiers",
                columns: new[] { "EventId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketTiers_EventId_Position",
                table: "TicketTiers",
                columns: new[] { "EventId", "Position" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentOrders_Coupons_CouponId",
                table: "PaymentOrders",
                column: "CouponId",
                principalTable: "Coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentOrders_TicketTiers_TicketTierId",
                table: "PaymentOrders",
                column: "TicketTierId",
                principalTable: "TicketTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentOrders_Coupons_CouponId",
                table: "PaymentOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentOrders_TicketTiers_TicketTierId",
                table: "PaymentOrders");

            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropTable(
                name: "TicketTiers");

            migrationBuilder.DropIndex(
                name: "IX_PaymentOrders_CouponId",
                table: "PaymentOrders");

            migrationBuilder.DropIndex(
                name: "IX_PaymentOrders_TicketTierId",
                table: "PaymentOrders");

            migrationBuilder.DropIndex(
                name: "IX_EventRegistrations_TicketCode",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "ShowLiveResults",
                table: "VotingCampaigns");

            migrationBuilder.DropColumn(
                name: "CouponId",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "DiscountAmountMinor",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "OriginalAmountMinor",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "TicketTierId",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "TicketCode",
                table: "EventRegistrations");
        }
    }
}
