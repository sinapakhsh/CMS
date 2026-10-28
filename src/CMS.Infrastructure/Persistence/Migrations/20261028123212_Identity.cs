using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Identity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "idn_users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                password_hash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                is_administrator = table.Column<bool>(type: "bit", nullable: false),
                failed_attempts = table.Column<int>(type: "int", nullable: false),
                locked_until_utc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_idn_users", x => x.id);
                table.UniqueConstraint("AK_idn_users_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "FK_idn_users_plt_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "plt_tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "idn_sessions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                refresh_hash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                consumed_hashes_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                expires_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                revoked_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_idn_sessions", x => x.id);
                table.CheckConstraint("ck_session_history", "ISJSON([consumed_hashes_json])=1");
                table.ForeignKey(
                    name: "FK_idn_sessions_idn_users_tenant_id_user_id",
                    columns: x => new { x.tenant_id, x.user_id },
                    principalTable: "idn_users",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_idn_sessions_tenant_id_user_id",
            table: "idn_sessions",
            columns: new[] { "tenant_id", "user_id" });

        migrationBuilder.CreateIndex(
            name: "IX_idn_users_tenant_id_email",
            table: "idn_users",
            columns: new[] { "tenant_id", "email" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "idn_sessions");

        migrationBuilder.DropTable(
            name: "idn_users");
    }
}
