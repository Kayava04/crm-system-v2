using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Students.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentStatusAndComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                schema: "students",
                table: "students",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "students",
                table: "students",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                schema: "students",
                table: "students");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "students",
                table: "students");
        }
    }
}
