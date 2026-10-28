using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialPlatform : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "plt_audit",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                actor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                entity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                entity_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                operation = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_plt_audit", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "plt_idempotency",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                operation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                fingerprint = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                result_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                expires_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_plt_idempotency", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "plt_outbox",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                event_type = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                occurred_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                next_attempt_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                lock_token = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                locked_until_utc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                attempts = table.Column<int>(type: "int", nullable: false),
                processed_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                dead_letter_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                last_error = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_plt_outbox", x => x.id);
                table.CheckConstraint("ck_outbox_payload", "ISJSON([payload]) = 1");
            });

        migrationBuilder.CreateTable(
            name: "plt_tenants",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                slug = table.Column<string>(type: "varchar(63)", unicode: false, maxLength: 63, nullable: false),
                name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_plt_tenants", x => x.id);
                table.CheckConstraint("ck_tenant_identity", "[id] = [tenant_id]");
            });

        migrationBuilder.CreateTable(
            name: "plt_locales",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                code = table.Column<string>(type: "varchar(35)", unicode: false, maxLength: 35, nullable: false),
                display_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                is_right_to_left = table.Column<bool>(type: "bit", nullable: false),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_plt_locales", x => x.id);
                table.ForeignKey(
                    name: "FK_plt_locales_plt_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "plt_tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_plt_audit_tenant_id_occurred_at_utc",
            table: "plt_audit",
            columns: new[] { "tenant_id", "occurred_at_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_plt_idempotency_expires_at_utc",
            table: "plt_idempotency",
            column: "expires_at_utc");

        migrationBuilder.CreateIndex(
            name: "IX_plt_idempotency_tenant_id_operation_key",
            table: "plt_idempotency",
            columns: new[] { "tenant_id", "operation", "key" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_plt_locales_tenant_id_code",
            table: "plt_locales",
            columns: new[] { "tenant_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_plt_outbox_event_id",
            table: "plt_outbox",
            column: "event_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_plt_outbox_next_attempt_at_utc_locked_until_utc",
            table: "plt_outbox",
            columns: new[] { "next_attempt_at_utc", "locked_until_utc" },
            filter: "[processed_at_utc] IS NULL AND [dead_letter_at_utc] IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_plt_tenants_slug",
            table: "plt_tenants",
            column: "slug",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "plt_audit");

        migrationBuilder.DropTable(
            name: "plt_idempotency");

        migrationBuilder.DropTable(
            name: "plt_locales");

        migrationBuilder.DropTable(
            name: "plt_outbox");

        migrationBuilder.DropTable(
            name: "plt_tenants");
    }
}
