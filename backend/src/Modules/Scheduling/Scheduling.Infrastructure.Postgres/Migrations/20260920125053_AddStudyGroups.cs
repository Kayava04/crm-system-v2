using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scheduling.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "EnrollmentId",
                schema: "scheduling",
                table: "schedules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                schema: "scheduling",
                table: "schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "study_groups",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "study_group_members",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_group_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_study_group_members_study_groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "scheduling",
                        principalTable: "study_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_schedules_GroupId",
                schema: "scheduling",
                table: "schedules",
                column: "GroupId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_schedules_target",
                schema: "scheduling",
                table: "schedules",
                sql: "(\"EnrollmentId\" IS NOT NULL) <> (\"GroupId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_study_group_members_EnrollmentId",
                schema: "scheduling",
                table: "study_group_members",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_study_group_members_GroupId_EnrollmentId",
                schema: "scheduling",
                table: "study_group_members",
                columns: new[] { "GroupId", "EnrollmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_study_groups_CourseId",
                schema: "scheduling",
                table: "study_groups",
                column: "CourseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "study_group_members",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "study_groups",
                schema: "scheduling");

            migrationBuilder.DropIndex(
                name: "IX_schedules_GroupId",
                schema: "scheduling",
                table: "schedules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_schedules_target",
                schema: "scheduling",
                table: "schedules");

            migrationBuilder.DropColumn(
                name: "GroupId",
                schema: "scheduling",
                table: "schedules");

            migrationBuilder.AlterColumn<Guid>(
                name: "EnrollmentId",
                schema: "scheduling",
                table: "schedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
