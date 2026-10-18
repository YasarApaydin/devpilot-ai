using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebChatAI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotaAndAbuseProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abuse_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    device_hash = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<string>(type: "text", nullable: false, defaultValue: "INFO"),
                    risk_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    details = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_abuse_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_abuse_audit_logs_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "device_registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    device_hash = table.Column<string>(type: "text", nullable: false),
                    last_ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    registration_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_registered_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    daily_registrations_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    risk_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_flagged = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    flag_reason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_device_registrations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_quotas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    daily_credit_limit = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    credits_used_today = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    cycle_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    subscription_tier = table.Column<string>(type: "text", nullable: false, defaultValue: "free"),
                    is_exempt_from_abuse_limits = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    allocated_by_device_hash = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_user_quotas", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_quotas_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_abuse_audit_logs_created_at",
                table: "abuse_audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_abuse_audit_logs_device_hash",
                table: "abuse_audit_logs",
                column: "device_hash");

            migrationBuilder.CreateIndex(
                name: "ix_abuse_audit_logs_event_type",
                table: "abuse_audit_logs",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_abuse_audit_logs_user_id",
                table: "abuse_audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_device_registrations_last_registered_date",
                table: "device_registrations",
                column: "last_registered_date");

            migrationBuilder.CreateIndex(
                name: "uq_device_registrations_device_hash",
                table: "device_registrations",
                column: "device_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_quotas_device_hash",
                table: "user_quotas",
                column: "allocated_by_device_hash");

            migrationBuilder.CreateIndex(
                name: "uq_user_quotas_user_id",
                table: "user_quotas",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abuse_audit_logs");

            migrationBuilder.DropTable(
                name: "device_registrations");

            migrationBuilder.DropTable(
                name: "user_quotas");
        }
    }
}
