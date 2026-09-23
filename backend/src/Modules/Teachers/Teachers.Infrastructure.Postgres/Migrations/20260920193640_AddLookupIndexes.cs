using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teachers.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_teachers_UserId",
                schema: "teachers",
                table: "teachers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_teachers_UserId",
                schema: "teachers",
                table: "teachers");
        }
    }
}
