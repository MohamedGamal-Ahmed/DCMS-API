using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingAnalysisFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "country",
                schema: "dcms",
                table: "meetings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_online",
                schema: "dcms",
                table: "meetings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "related_partner",
                schema: "dcms",
                table: "meetings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "related_project",
                schema: "dcms",
                table: "meetings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "country",
                schema: "dcms",
                table: "meetings");

            migrationBuilder.DropColumn(
                name: "is_online",
                schema: "dcms",
                table: "meetings");

            migrationBuilder.DropColumn(
                name: "related_partner",
                schema: "dcms",
                table: "meetings");

            migrationBuilder.DropColumn(
                name: "related_project",
                schema: "dcms",
                table: "meetings");
        }
    }
}
