using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Content : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddUniqueConstraint(
            name: "AK_plt_locales_tenant_id_id",
            table: "plt_locales",
            columns: new[] { "tenant_id", "id" });

        migrationBuilder.CreateTable(
            name: "cnt_types",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                key = table.Column<string>(type: "nvarchar(63)", maxLength: 63, nullable: false),
                name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                fields_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cnt_types", x => x.id);
                table.UniqueConstraint("AK_cnt_types_tenant_id_id", x => new { x.tenant_id, x.id });
                table.CheckConstraint("ck_type_fields", "ISJSON([fields_json])=1");
                table.ForeignKey(
                    name: "FK_cnt_types_plt_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "plt_tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "cnt_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                content_type_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                locale_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                fields_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                publish_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cnt_entries", x => x.id);
                table.UniqueConstraint("AK_cnt_entries_tenant_id_id", x => new { x.tenant_id, x.id });
                table.CheckConstraint("ck_entry_fields", "ISJSON([fields_json])=1");
                table.ForeignKey(
                    name: "FK_cnt_entries_cnt_types_tenant_id_content_type_id",
                    columns: x => new { x.tenant_id, x.content_type_id },
                    principalTable: "cnt_types",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_cnt_entries_plt_locales_tenant_id_locale_id",
                    columns: x => new { x.tenant_id, x.locale_id },
                    principalTable: "plt_locales",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "cnt_redirects",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                entry_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                locale_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cnt_redirects", x => x.id);
                table.ForeignKey(
                    name: "FK_cnt_redirects_cnt_entries_tenant_id_entry_id",
                    columns: x => new { x.tenant_id, x.entry_id },
                    principalTable: "cnt_entries",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "cnt_revisions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                entry_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                version = table.Column<int>(type: "int", nullable: false),
                slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                fields_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cnt_revisions", x => x.id);
                table.CheckConstraint("ck_revision_fields", "ISJSON([fields_json])=1");
                table.ForeignKey(
                    name: "FK_cnt_revisions_cnt_entries_tenant_id_entry_id",
                    columns: x => new { x.tenant_id, x.entry_id },
                    principalTable: "cnt_entries",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_cnt_entries_tenant_id_content_type_id",
            table: "cnt_entries",
            columns: new[] { "tenant_id", "content_type_id" });

        migrationBuilder.CreateIndex(
            name: "IX_cnt_entries_tenant_id_locale_id_slug",
            table: "cnt_entries",
            columns: new[] { "tenant_id", "locale_id", "slug" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_cnt_redirects_tenant_id_entry_id",
            table: "cnt_redirects",
            columns: new[] { "tenant_id", "entry_id" });

        migrationBuilder.CreateIndex(
            name: "IX_cnt_redirects_tenant_id_locale_id_slug",
            table: "cnt_redirects",
            columns: new[] { "tenant_id", "locale_id", "slug" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_cnt_revisions_tenant_id_entry_id_version",
            table: "cnt_revisions",
            columns: new[] { "tenant_id", "entry_id", "version" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_cnt_types_tenant_id_key",
            table: "cnt_types",
            columns: new[] { "tenant_id", "key" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "cnt_redirects");

        migrationBuilder.DropTable(
            name: "cnt_revisions");

        migrationBuilder.DropTable(
            name: "cnt_entries");

        migrationBuilder.DropTable(
            name: "cnt_types");

        migrationBuilder.DropUniqueConstraint(
            name: "AK_plt_locales_tenant_id_id",
            table: "plt_locales");
    }
}
