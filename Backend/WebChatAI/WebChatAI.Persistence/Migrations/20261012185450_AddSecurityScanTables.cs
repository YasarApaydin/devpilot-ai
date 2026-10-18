using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebChatAI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityScanTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "security_scans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_url = table.Column<string>(type: "text", nullable: false),
                    normalized_target_url = table.Column<string>(type: "text", nullable: false),
                    scan_mode = table.Column<string>(type: "text", nullable: false, defaultValue: "quick"),
                    selected_categories = table.Column<string>(type: "text", nullable: false),
                    custom_instructions = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    total_findings_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    critical_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    high_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    medium_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    low_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    info_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    strix_run_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_scans", x => x.id);
                    table.ForeignKey(
                        name: "fk_security_scans_conversation",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_security_scans_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "security_scan_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    security_scan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_url = table.Column<string>(type: "text", nullable: false),
                    disclaimer_version = table.Column<string>(type: "text", nullable: false, defaultValue: "v1.0"),
                    has_ownership_authorization = table.Column<bool>(type: "boolean", nullable: false),
                    has_risk_acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    client_ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    consented_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_scan_consents", x => x.id);
                    table.ForeignKey(
                        name: "fk_security_scan_consents_security_scan",
                        column: x => x.security_scan_id,
                        principalTable: "security_scans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_security_scan_consents_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "security_scan_findings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    security_scan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<string>(type: "text", nullable: false, defaultValue: "info"),
                    cwe = table.Column<string>(type: "text", nullable: true),
                    cvss_score = table.Column<double>(type: "double precision", nullable: true),
                    cvss_vector = table.Column<string>(type: "text", nullable: true),
                    target_url = table.Column<string>(type: "text", nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    impact = table.Column<string>(type: "text", nullable: true),
                    technical_analysis = table.Column<string>(type: "text", nullable: true),
                    remediation = table.Column<string>(type: "text", nullable: true),
                    evidence = table.Column<string>(type: "text", nullable: true),
                    poc_description = table.Column<string>(type: "text", nullable: true),
                    confidence = table.Column<string>(type: "text", nullable: true),
                    strix_vuln_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_scan_findings", x => x.id);
                    table.ForeignKey(
                        name: "fk_security_scan_findings_security_scan",
                        column: x => x.security_scan_id,
                        principalTable: "security_scans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_security_scan_consents_user_id",
                table: "security_scan_consents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_security_scan_consents_scan_id",
                table: "security_scan_consents",
                column: "security_scan_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_security_scan_findings_scan_id",
                table: "security_scan_findings",
                column: "security_scan_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_scan_findings_severity",
                table: "security_scan_findings",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "IX_security_scans_conversation_id",
                table: "security_scans",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_scans_created_at",
                table: "security_scans",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_security_scans_status",
                table: "security_scans",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_security_scans_user_id",
                table: "security_scans",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "security_scan_consents");

            migrationBuilder.DropTable(
                name: "security_scan_findings");

            migrationBuilder.DropTable(
                name: "security_scans");
        }
    }
}
