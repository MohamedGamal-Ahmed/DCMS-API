using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInboundFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attachment_urls",
                schema: "dcms",
                table: "inbound");

            migrationBuilder.AddColumn<string>(
                name: "attachment_url",
                schema: "dcms",
                table: "inbound",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attachment_url",
                schema: "dcms",
                table: "inbound");

            migrationBuilder.AddColumn<List<string>>(
                name: "attachment_urls",
                schema: "dcms",
                table: "inbound",
                type: "text[]",
                nullable: false);
        }
    }
}
