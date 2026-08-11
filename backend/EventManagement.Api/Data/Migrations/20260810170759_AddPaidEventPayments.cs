using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidEventPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Events",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "GHS");

            migrationBuilder.AddColumn<long>(
                name: "PriceMinor",
                table: "Events",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentOrderId",
                table: "EventRegistrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_PaymentOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentOrders_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentOrders_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentWebhookReceipts",
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
                    table.PrimaryKey("PK_PaymentWebhookReceipts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_PaymentOrderId",
                table: "EventRegistrations",
                column: "PaymentOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_EventId_StudentId_Status",
                table: "PaymentOrders",
                columns: new[] { "EventId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_ProviderReference",
                table: "PaymentOrders",
                column: "ProviderReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_Status_ExpiresAt",
                table: "PaymentOrders",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_StudentId",
                table: "PaymentOrders",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookReceipts_ProcessedAt",
                table: "PaymentWebhookReceipts",
                column: "ProcessedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_EventRegistrations_PaymentOrders_PaymentOrderId",
                table: "EventRegistrations",
                column: "PaymentOrderId",
                principalTable: "PaymentOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventRegistrations_PaymentOrders_PaymentOrderId",
                table: "EventRegistrations");

            migrationBuilder.DropTable(
                name: "PaymentOrders");

            migrationBuilder.DropTable(
                name: "PaymentWebhookReceipts");

            migrationBuilder.DropIndex(
                name: "IX_EventRegistrations_PaymentOrderId",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PriceMinor",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PaymentOrderId",
                table: "EventRegistrations");
        }
    }
}
