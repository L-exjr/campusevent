using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminAuditLogs_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_ActorUserId",
                table: "AdminAuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_CreatedAt",
                table: "AdminAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_TargetType_TargetId",
                table: "AdminAuditLogs",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.Sql(
                """
                CREATE FUNCTION prevent_admin_audit_log_mutation()
                RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'Admin audit logs are immutable';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER admin_audit_logs_immutable
                BEFORE UPDATE OR DELETE ON "AdminAuditLogs"
                FOR EACH ROW EXECUTE FUNCTION prevent_admin_audit_log_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS admin_audit_logs_immutable ON "AdminAuditLogs";
                DROP FUNCTION IF EXISTS prevent_admin_audit_log_mutation();
                """);

            migrationBuilder.DropTable(
                name: "AdminAuditLogs");
        }
    }
}
