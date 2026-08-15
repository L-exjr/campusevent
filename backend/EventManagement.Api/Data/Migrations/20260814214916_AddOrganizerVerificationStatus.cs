using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizerVerificationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerificationStatus",
                table: "Users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unverified");

            // Preserve the legacy application history while translating its latest
            // trust decision into the new non-authorizing profile signal.
            migrationBuilder.Sql("""
                UPDATE "Users" AS u
                SET "VerificationStatus" = CASE
                    WHEN EXISTS (
                        SELECT 1 FROM "OrganizerApplications" AS a
                        WHERE a."UserId" = u."Id" AND a."Status" = 'Approved'
                    ) THEN 'Verified'
                    WHEN EXISTS (
                        SELECT 1 FROM "OrganizerApplications" AS a
                        WHERE a."UserId" = u."Id" AND a."Status" = 'Pending'
                    ) THEN 'Pending'
                    ELSE 'Unverified'
                END
                WHERE u."Role" <> 'Admin';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "Users");
        }
    }
}
