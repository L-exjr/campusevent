using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImageLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageObjectKey",
                table: "Users",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageObjectKey",
                table: "Events",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImageUploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Bucket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    PublicUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletionClaimedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeleteAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageUploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageUploads_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageUploads_Bucket_ObjectKey",
                table: "ImageUploads",
                columns: new[] { "Bucket", "ObjectKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageUploads_DeletionClaimedBy",
                table: "ImageUploads",
                column: "DeletionClaimedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ImageUploads_OwnerId",
                table: "ImageUploads",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageUploads_PublicUrl",
                table: "ImageUploads",
                column: "PublicUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageUploads_Status_AvailableAt",
                table: "ImageUploads",
                columns: new[] { "Status", "AvailableAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageUploads");

            migrationBuilder.DropColumn(
                name: "ImageObjectKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ImageObjectKey",
                table: "Events");
        }
    }
}
