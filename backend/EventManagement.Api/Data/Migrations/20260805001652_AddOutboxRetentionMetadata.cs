using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxRetentionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRetriedAt",
                table: "EmailOutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LifetimeAttemptCount",
                table: "EmailOutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ManualRetryCount",
                table: "EmailOutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRetriedAt",
                table: "EmailOutboxMessages");

            migrationBuilder.DropColumn(
                name: "LifetimeAttemptCount",
                table: "EmailOutboxMessages");

            migrationBuilder.DropColumn(
                name: "ManualRetryCount",
                table: "EmailOutboxMessages");
        }
    }
}
