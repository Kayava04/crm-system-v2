using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enrollments.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredSchedule : Migration
    {
        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredSchedule",
                schema: "enrollments",
                table: "enrollments");
        }
    }
}
