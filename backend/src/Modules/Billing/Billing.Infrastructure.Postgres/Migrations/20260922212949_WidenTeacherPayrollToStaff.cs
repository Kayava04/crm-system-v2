using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class WidenTeacherPayrollToStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TeacherId",
                schema: "billing",
                table: "teacher_payrolls",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                schema: "billing",
                table: "teacher_payrolls",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_teacher_payrolls_UserId_Period",
                schema: "billing",
                table: "teacher_payrolls",
                columns: new[] { "UserId", "Period" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_teacher_payrolls_target",
                schema: "billing",
                table: "teacher_payrolls",
                sql: "(\"TeacherId\" IS NOT NULL) <> (\"UserId\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_teacher_payrolls_UserId_Period",
                schema: "billing",
                table: "teacher_payrolls");

            migrationBuilder.DropCheckConstraint(
                name: "ck_teacher_payrolls_target",
                schema: "billing",
                table: "teacher_payrolls");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "billing",
                table: "teacher_payrolls");

            migrationBuilder.AlterColumn<Guid>(
                name: "TeacherId",
                schema: "billing",
                table: "teacher_payrolls",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
