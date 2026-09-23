using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scheduling.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonCancellationReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "scheduling",
                table: "schedules",
                type: "text",
                nullable: false,
                defaultValue: "None");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "scheduling",
                table: "schedules");
        }
    }
}
