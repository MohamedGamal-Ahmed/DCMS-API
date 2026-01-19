using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using DCMS.Infrastructure.Data;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    [DbContext(typeof(DCMSDbContext))]
    [Migration("20251210153000_AddReplyAttachmentToInbound")]
    public partial class AddReplyAttachmentToInbound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ReplyAttachmentUrl to inbound table
            migrationBuilder.AddColumn<string>(
                name: "ReplyAttachmentUrl",
                schema: "dcms",
                table: "inbound",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove ReplyAttachmentUrl from inbound
            migrationBuilder.DropColumn(
                name: "ReplyAttachmentUrl",
                schema: "dcms",
                table: "inbound");
        }
    }
}
