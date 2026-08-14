using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizerDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOrganizerDirectoryVisible",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerBannerObjectKey",
                table: "Users",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerBannerUrl",
                table: "Users",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerBio",
                table: "Users",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerFacebookUrl",
                table: "Users",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerInstagramUrl",
                table: "Users",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerTwitterUrl",
                table: "Users",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerWebsiteUrl",
                table: "Users",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedOrganizerId",
                table: "BookingRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrganizerSpecialties",
                columns: table => new
                {
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizerSpecialties", x => new { x.OrganizerId, x.Category });
                    table.ForeignKey(
                        name: "FK_OrganizerSpecialties_Users_OrganizerId",
                        column: x => x.OrganizerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_RequestedOrganizerId",
                table: "BookingRequests",
                column: "RequestedOrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizerSpecialties_Category",
                table: "OrganizerSpecialties",
                column: "Category");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingRequests_Users_RequestedOrganizerId",
                table: "BookingRequests",
                column: "RequestedOrganizerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingRequests_Users_RequestedOrganizerId",
                table: "BookingRequests");

            migrationBuilder.DropTable(
                name: "OrganizerSpecialties");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_RequestedOrganizerId",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "IsOrganizerDirectoryVisible",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizerBannerObjectKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizerBannerUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizerBio",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizerFacebookUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizerInstagramUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizerTwitterUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizerWebsiteUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RequestedOrganizerId",
                table: "BookingRequests");
        }
    }
}
