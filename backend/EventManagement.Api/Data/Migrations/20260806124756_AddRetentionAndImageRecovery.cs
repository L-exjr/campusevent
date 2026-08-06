using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRetentionAndImageRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRetriedAt",
                table: "ImageUploads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LifetimeDeleteAttemptCount",
                table: "ImageUploads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ManualRetryCount",
                table: "ImageUploads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PersonalDataAnonymizedAt",
                table: "BookingRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "ImageUploads"
                SET "LifetimeDeleteAttemptCount" = "DeleteAttemptCount";

                CREATE OR REPLACE FUNCTION prevent_admin_audit_log_mutation()
                RETURNS trigger AS $$
                BEGIN
                    IF TG_OP = 'DELETE'
                       AND current_setting('app.audit_retention_cleanup', true) = 'on' THEN
                        RETURN OLD;
                    END IF;
                    RAISE EXCEPTION 'Admin audit logs are immutable';
                END;
                $$ LANGUAGE plpgsql;

                CREATE EXTENSION IF NOT EXISTS pg_trgm;
                CREATE INDEX "IX_AdminAuditLogs_Action_Trgm"
                    ON "AdminAuditLogs" USING gin (lower("Action") gin_trgm_ops);
                CREATE INDEX "IX_AdminAuditLogs_TargetType_Trgm"
                    ON "AdminAuditLogs" USING gin (lower("TargetType") gin_trgm_ops);
                CREATE INDEX "IX_AdminAuditLogs_TargetId_Trgm"
                    ON "AdminAuditLogs" USING gin (lower("TargetId") gin_trgm_ops);
                CREATE INDEX "IX_Users_Name_Trgm"
                    ON "Users" USING gin (lower("Name") gin_trgm_ops);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_Status_UpdatedAt",
                table: "BookingRequests",
                columns: new[] { "Status", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_AdminAuditLogs_Action_Trgm";
                DROP INDEX IF EXISTS "IX_AdminAuditLogs_TargetType_Trgm";
                DROP INDEX IF EXISTS "IX_AdminAuditLogs_TargetId_Trgm";
                DROP INDEX IF EXISTS "IX_Users_Name_Trgm";

                CREATE OR REPLACE FUNCTION prevent_admin_audit_log_mutation()
                RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'Admin audit logs are immutable';
                END;
                $$ LANGUAGE plpgsql;
                """);
            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_Status_UpdatedAt",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "LastRetriedAt",
                table: "ImageUploads");

            migrationBuilder.DropColumn(
                name: "LifetimeDeleteAttemptCount",
                table: "ImageUploads");

            migrationBuilder.DropColumn(
                name: "ManualRetryCount",
                table: "ImageUploads");

            migrationBuilder.DropColumn(
                name: "PersonalDataAnonymizedAt",
                table: "BookingRequests");
        }
    }
}
