using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notifications.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationReferenceKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceKey",
                schema: "notifications",
                table: "notifications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_RecipientUserId_Type_ReferenceKey",
                schema: "notifications",
                table: "notifications",
                columns: new[] { "RecipientUserId", "Type", "ReferenceKey" },
                unique: true,
                filter: "\"ReferenceKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_RecipientUserId_Type_ReferenceKey",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ReferenceKey",
                schema: "notifications",
                table: "notifications");
        }
    }
}
