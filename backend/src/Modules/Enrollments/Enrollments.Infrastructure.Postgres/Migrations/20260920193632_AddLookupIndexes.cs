using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enrollments.Infrastructure.Postgres.Migrations
{
    public partial class AddLookupIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_enrollments_CourseId",
                schema: "enrollments",
                table: "enrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_StudentId",
                schema: "enrollments",
                table: "enrollments",
                column: "StudentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_enrollments_CourseId",
                schema: "enrollments",
                table: "enrollments");

            migrationBuilder.DropIndex(
                name: "IX_enrollments_StudentId",
                schema: "enrollments",
                table: "enrollments");
        }
    }
}
