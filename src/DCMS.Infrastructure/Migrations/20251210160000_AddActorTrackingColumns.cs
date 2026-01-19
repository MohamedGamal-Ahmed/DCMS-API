using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using DCMS.Infrastructure.Data;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    [DbContext(typeof(DCMSDbContext))]
    [Migration("20251210160000_AddActorTrackingColumns")]
    public partial class AddActorTrackingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add UpdatedByUserId to Inbound
            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                schema: "dcms",
                table: "inbound",
                type: "integer",
                nullable: true);

            // Add CreatedByUserId to InboundTransfer
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                schema: "dcms",
                table: "inbound_transfers",
                type: "integer",
                nullable: true);

            // Add Foreign Keys
            migrationBuilder.CreateIndex(
                name: "IX_inbound_UpdatedByUserId",
                schema: "dcms",
                table: "inbound",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_transfers_CreatedByUserId",
                schema: "dcms",
                table: "inbound_transfers",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_inbound_users_UpdatedByUserId",
                schema: "dcms",
                table: "inbound",
                column: "UpdatedByUserId",
                principalSchema: "dcms",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inbound_transfers_users_CreatedByUserId",
                schema: "dcms",
                table: "inbound_transfers",
                column: "CreatedByUserId",
                principalSchema: "dcms",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inbound_users_UpdatedByUserId",
                schema: "dcms",
                table: "inbound");

            migrationBuilder.DropForeignKey(
                name: "FK_inbound_transfers_users_CreatedByUserId",
                schema: "dcms",
                table: "inbound_transfers");

            migrationBuilder.DropIndex(
                name: "IX_inbound_UpdatedByUserId",
                schema: "dcms",
                table: "inbound");

            migrationBuilder.DropIndex(
                name: "IX_inbound_transfers_CreatedByUserId",
                schema: "dcms",
                table: "inbound_transfers");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                schema: "dcms",
                table: "inbound");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "dcms",
                table: "inbound_transfers");
        }
    }
}
