using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Roles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "role_ids_json",
            table: "idn_users",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.CreateTable(
            name: "idn_roles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                permissions_json = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_idn_roles", x => x.id);
                table.CheckConstraint("ck_role_permissions", "ISJSON([permissions_json])=1");
                table.ForeignKey(
                    name: "FK_idn_roles_plt_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "plt_tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddCheckConstraint(
            name: "ck_user_roles",
            table: "idn_users",
            sql: "ISJSON([role_ids_json])=1");

        migrationBuilder.CreateIndex(
            name: "IX_idn_roles_tenant_id_name",
            table: "idn_roles",
            columns: new[] { "tenant_id", "name" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "idn_roles");

        migrationBuilder.DropCheckConstraint(
            name: "ck_user_roles",
            table: "idn_users");

        migrationBuilder.DropColumn(
            name: "role_ids_json",
            table: "idn_users");
    }
}
