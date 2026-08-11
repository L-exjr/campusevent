using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CertificateGeneratedAt",
                table: "EventRegistrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateObjectKey",
                table: "EventRegistrations",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CertificateTemplateVersion",
                table: "EventRegistrations",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateGeneratedAt",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "CertificateObjectKey",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "CertificateTemplateVersion",
                table: "EventRegistrations");
        }
    }
}
