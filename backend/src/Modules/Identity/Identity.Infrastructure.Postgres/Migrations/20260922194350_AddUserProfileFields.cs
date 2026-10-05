using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Postgres.Migrations
{
    public partial class AddUserProfileFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "identity",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "identity",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                schema: "identity",
                table: "users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                schema: "identity",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Salary",
                schema: "identity",
                table: "users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Salary",
                schema: "identity",
                table: "users");
        }
    }
}
