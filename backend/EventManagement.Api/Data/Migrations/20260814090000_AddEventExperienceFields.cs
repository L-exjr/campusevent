using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using EventManagement.Api.Data;

#nullable disable

namespace EventManagement.Api.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260814090000_AddEventExperienceFields")]
public partial class AddEventExperienceFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(name: "EndDate", table: "Events", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "VirtualPlatform", table: "Events", type: "character varying(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<double>(name: "Latitude", table: "Events", type: "double precision", nullable: true);
        migrationBuilder.AddColumn<double>(name: "Longitude", table: "Events", type: "double precision", nullable: true);
        foreach (var name in new[] { "InstagramUrl", "TwitterUrl", "FacebookUrl", "WebsiteUrl" })
            migrationBuilder.AddColumn<string>(name: name, table: "Events", type: "character varying(2048)", maxLength: 2048, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "TicketingEnabled", table: "Events", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "RegistrationsEnabled", table: "Events", type: "boolean", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<bool>(name: "VotingEnabled", table: "Events", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.Sql("UPDATE \"Events\" SET \"EndDate\" = \"Date\" + interval '1 hour', \"TicketingEnabled\" = (\"PriceMinor\" > 0), \"RegistrationsEnabled\" = (\"PriceMinor\" = 0)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var name in new[] { "EndDate", "VirtualPlatform", "Latitude", "Longitude", "InstagramUrl", "TwitterUrl", "FacebookUrl", "WebsiteUrl", "TicketingEnabled", "RegistrationsEnabled", "VotingEnabled" })
            migrationBuilder.DropColumn(name: name, table: "Events");
    }
}
