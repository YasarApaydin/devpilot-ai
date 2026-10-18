using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebChatAI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveSecurityScanUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "uq_security_scans_active_per_user",
                table: "security_scans",
                column: "user_id",
                unique: true,
                filter: "status IN ('Running', 'Pending') AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_security_scans_active_per_user",
                table: "security_scans");
        }
    }
}
