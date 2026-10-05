using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enrollments.Infrastructure.Postgres.Migrations
{
    public partial class AddPreferredSchedule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredSchedule",
                schema: "enrollments",
                table: "enrollments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredSchedule",
                schema: "enrollments",
                table: "enrollments");
        }
    }
}
