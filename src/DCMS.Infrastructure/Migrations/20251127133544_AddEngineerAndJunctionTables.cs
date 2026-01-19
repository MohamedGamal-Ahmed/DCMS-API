using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEngineerAndJunctionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "engineers",
                schema: "dcms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engineers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbound_responsible_engineers",
                schema: "dcms",
                columns: table => new
                {
                    inbound_id = table.Column<int>(type: "integer", nullable: false),
                    engineer_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbound_responsible_engineers", x => new { x.inbound_id, x.engineer_id });
                    table.ForeignKey(
                        name: "FK_inbound_responsible_engineers_engineers_engineer_id",
                        column: x => x.engineer_id,
                        principalSchema: "dcms",
                        principalTable: "engineers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inbound_responsible_engineers_inbound_inbound_id",
                        column: x => x.inbound_id,
                        principalSchema: "dcms",
                        principalTable: "inbound",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inbound_transfers",
                schema: "dcms",
                columns: table => new
                {
                    inbound_id = table.Column<int>(type: "integer", nullable: false),
                    engineer_id = table.Column<int>(type: "integer", nullable: false),
                    transfer_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbound_transfers", x => new { x.inbound_id, x.engineer_id });
                    table.ForeignKey(
                        name: "FK_inbound_transfers_engineers_engineer_id",
                        column: x => x.engineer_id,
                        principalSchema: "dcms",
                        principalTable: "engineers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inbound_transfers_inbound_inbound_id",
                        column: x => x.inbound_id,
                        principalSchema: "dcms",
                        principalTable: "inbound",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_engineers_full_name",
                schema: "dcms",
                table: "engineers",
                column: "full_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inbound_responsible_engineers_engineer_id",
                schema: "dcms",
                table: "inbound_responsible_engineers",
                column: "engineer_id");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_transfers_engineer_id",
                schema: "dcms",
                table: "inbound_transfers",
                column: "engineer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbound_responsible_engineers",
                schema: "dcms");

            migrationBuilder.DropTable(
                name: "inbound_transfers",
                schema: "dcms");

            migrationBuilder.DropTable(
                name: "engineers",
                schema: "dcms");
        }
    }
}
