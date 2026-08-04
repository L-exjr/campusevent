using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributedAuthRateLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthRateLimitBuckets",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    WindowStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthRateLimitBuckets", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthRateLimitBuckets_UpdatedAt",
                table: "AuthRateLimitBuckets",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthRateLimitBuckets");
        }
    }
}
