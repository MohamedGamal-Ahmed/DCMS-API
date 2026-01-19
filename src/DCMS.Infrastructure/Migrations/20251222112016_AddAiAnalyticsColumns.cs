using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiAnalyticsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "completion_tokens",
                schema: "dcms",
                table: "ai_request_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_success",
                schema: "dcms",
                table: "ai_request_logs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "prompt_tokens",
                schema: "dcms",
                table: "ai_request_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "seconds_saved",
                schema: "dcms",
                table: "ai_request_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "user_feedback",
                schema: "dcms",
                table: "ai_request_logs",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "completion_tokens",
                schema: "dcms",
                table: "ai_request_logs");

            migrationBuilder.DropColumn(
                name: "is_success",
                schema: "dcms",
                table: "ai_request_logs");

            migrationBuilder.DropColumn(
                name: "prompt_tokens",
                schema: "dcms",
                table: "ai_request_logs");

            migrationBuilder.DropColumn(
                name: "seconds_saved",
                schema: "dcms",
                table: "ai_request_logs");

            migrationBuilder.DropColumn(
                name: "user_feedback",
                schema: "dcms",
                table: "ai_request_logs");
        }
    }
}
