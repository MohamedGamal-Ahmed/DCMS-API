using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using DCMS.Infrastructure.Data;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    [DbContext(typeof(DCMSDbContext))]
    [Migration("20251210150000_AddThreeTierAttachments")]
    public partial class AddThreeTierAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add OriginalAttachmentUrl to inbound table
            migrationBuilder.AddColumn<string>(
                name: "OriginalAttachmentUrl",
                schema: "dcms",
                table: "inbound",
                type: "text",
                nullable: true);

            // Add TransferAttachmentUrl to inbound_transfers table
            migrationBuilder.AddColumn<string>(
                name: "TransferAttachmentUrl",
                schema: "dcms",
                table: "inbound_transfers",
                type: "text",
                nullable: true);

            // Add ResponseAttachmentUrl to inbound_transfers table
            migrationBuilder.AddColumn<string>(
                name: "ResponseAttachmentUrl",
                schema: "dcms",
                table: "inbound_transfers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove OriginalAttachmentUrl from inbound
            migrationBuilder.DropColumn(
                name: "OriginalAttachmentUrl",
                schema: "dcms",
                table: "inbound");

            // Remove TransferAttachmentUrl from inbound_transfers
            migrationBuilder.DropColumn(
                name: "TransferAttachmentUrl",
                schema: "dcms",
                table: "inbound_transfers");

            // Remove ResponseAttachmentUrl from inbound_transfers
            migrationBuilder.DropColumn(
                name: "ResponseAttachmentUrl",
                schema: "dcms",
                table: "inbound_transfers");
        }
    }
}
