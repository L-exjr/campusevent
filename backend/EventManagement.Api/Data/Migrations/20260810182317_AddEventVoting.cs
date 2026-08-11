using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventVoting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VotingCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpensAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosesAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VotingCampaigns_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VotingWebhookReceipts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingWebhookReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VotingCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PricePerVoteMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "GHS"),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VotingCategories_VotingCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "VotingCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VotingNominees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingNominees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VotingNominees_VotingCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "VotingCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VotingPaymentOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomineeId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AuthorizationUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotingPaymentOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VotingPaymentOrders_Users_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VotingPaymentOrders_VotingCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "VotingCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VotingPaymentOrders_VotingNominees_NomineeId",
                        column: x => x.NomineeId,
                        principalTable: "VotingNominees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VoteRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomineeId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    VotingPaymentOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CastAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoteRecords_Users_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoteRecords_VotingCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "VotingCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoteRecords_VotingNominees_NomineeId",
                        column: x => x.NomineeId,
                        principalTable: "VotingNominees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoteRecords_VotingPaymentOrders_VotingPaymentOrderId",
                        column: x => x.VotingPaymentOrderId,
                        principalTable: "VotingPaymentOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoteRecords_CategoryId",
                table: "VoteRecords",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteRecords_CategoryId_VoterId",
                table: "VoteRecords",
                columns: new[] { "CategoryId", "VoterId" },
                unique: true,
                filter: "\"VotingPaymentOrderId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VoteRecords_NomineeId_CastAt",
                table: "VoteRecords",
                columns: new[] { "NomineeId", "CastAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VoteRecords_VoterId",
                table: "VoteRecords",
                column: "VoterId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteRecords_VotingPaymentOrderId",
                table: "VoteRecords",
                column: "VotingPaymentOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingCampaigns_EventId",
                table: "VotingCampaigns",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingCampaigns_IsPublished_OpensAt_ClosesAt",
                table: "VotingCampaigns",
                columns: new[] { "IsPublished", "OpensAt", "ClosesAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VotingCategories_CampaignId_Position",
                table: "VotingCategories",
                columns: new[] { "CampaignId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingNominees_CategoryId_Position",
                table: "VotingNominees",
                columns: new[] { "CategoryId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingPaymentOrders_CategoryId_VoterId_Status",
                table: "VotingPaymentOrders",
                columns: new[] { "CategoryId", "VoterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VotingPaymentOrders_NomineeId",
                table: "VotingPaymentOrders",
                column: "NomineeId");

            migrationBuilder.CreateIndex(
                name: "IX_VotingPaymentOrders_ProviderReference",
                table: "VotingPaymentOrders",
                column: "ProviderReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VotingPaymentOrders_VoterId",
                table: "VotingPaymentOrders",
                column: "VoterId");

            migrationBuilder.CreateIndex(
                name: "IX_VotingWebhookReceipts_ProcessedAt",
                table: "VotingWebhookReceipts",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoteRecords");

            migrationBuilder.DropTable(
                name: "VotingWebhookReceipts");

            migrationBuilder.DropTable(
                name: "VotingPaymentOrders");

            migrationBuilder.DropTable(
                name: "VotingNominees");

            migrationBuilder.DropTable(
                name: "VotingCategories");

            migrationBuilder.DropTable(
                name: "VotingCampaigns");
        }
    }
}
