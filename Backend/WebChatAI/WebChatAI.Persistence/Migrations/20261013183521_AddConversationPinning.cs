using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebChatAI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationPinning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_pinned",
                table: "conversations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "pinned_at",
                table: "conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_conversations_user_id_is_pinned",
                table: "conversations",
                columns: new[] { "user_id", "is_pinned" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_conversations_user_id_is_pinned",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "is_pinned",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "pinned_at",
                table: "conversations");
        }
    }
}
