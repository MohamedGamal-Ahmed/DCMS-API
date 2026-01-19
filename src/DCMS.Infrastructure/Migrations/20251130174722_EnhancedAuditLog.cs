using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_log_users_changed_by",
                schema: "dcms",
                table: "audit_log");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_log",
                schema: "dcms",
                table: "audit_log");

            migrationBuilder.DropColumn(
                name: "field_name",
                schema: "dcms",
                table: "audit_log");

            migrationBuilder.DropColumn(
                name: "new_value",
                schema: "dcms",
                table: "audit_log");

            migrationBuilder.DropColumn(
                name: "record_type",
                schema: "dcms",
                table: "audit_log");

            migrationBuilder.RenameTable(
                name: "audit_log",
                schema: "dcms",
                newName: "audit_logs",
                newSchema: "dcms");

            migrationBuilder.RenameColumn(
                name: "record_id",
                schema: "dcms",
                table: "audit_logs",
                newName: "entity_type");

            migrationBuilder.RenameColumn(
                name: "old_value",
                schema: "dcms",
                table: "audit_logs",
                newName: "old_values");

            migrationBuilder.RenameColumn(
                name: "notes",
                schema: "dcms",
                table: "audit_logs",
                newName: "new_values");

            migrationBuilder.RenameColumn(
                name: "changed_by",
                schema: "dcms",
                table: "audit_logs",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "changed_at",
                schema: "dcms",
                table: "audit_logs",
                newName: "timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_audit_log_changed_by",
                schema: "dcms",
                table: "audit_logs",
                newName: "IX_audit_logs_UserId");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                schema: "dcms",
                table: "audit_logs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "dcms",
                table: "audit_logs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "entity_id",
                schema: "dcms",
                table: "audit_logs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                schema: "dcms",
                table: "audit_logs",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_name",
                schema: "dcms",
                table: "audit_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_logs",
                schema: "dcms",
                table: "audit_logs",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action",
                schema: "dcms",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_type",
                schema: "dcms",
                table: "audit_logs",
                column: "entity_type");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_timestamp",
                schema: "dcms",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_name",
                schema: "dcms",
                table: "audit_logs",
                column: "user_name");

            migrationBuilder.AddForeignKey(
                name: "FK_audit_logs_users_UserId",
                schema: "dcms",
                table: "audit_logs",
                column: "UserId",
                principalSchema: "dcms",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_logs_users_UserId",
                schema: "dcms",
                table: "audit_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_logs",
                schema: "dcms",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_action",
                schema: "dcms",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_entity_type",
                schema: "dcms",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_timestamp",
                schema: "dcms",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_user_name",
                schema: "dcms",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "dcms",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "entity_id",
                schema: "dcms",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "ip_address",
                schema: "dcms",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "user_name",
                schema: "dcms",
                table: "audit_logs");

            migrationBuilder.RenameTable(
                name: "audit_logs",
                schema: "dcms",
                newName: "audit_log",
                newSchema: "dcms");

            migrationBuilder.RenameColumn(
                name: "timestamp",
                schema: "dcms",
                table: "audit_log",
                newName: "changed_at");

            migrationBuilder.RenameColumn(
                name: "old_values",
                schema: "dcms",
                table: "audit_log",
                newName: "old_value");

            migrationBuilder.RenameColumn(
                name: "new_values",
                schema: "dcms",
                table: "audit_log",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "entity_type",
                schema: "dcms",
                table: "audit_log",
                newName: "record_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "dcms",
                table: "audit_log",
                newName: "changed_by");

            migrationBuilder.RenameIndex(
                name: "IX_audit_logs_UserId",
                schema: "dcms",
                table: "audit_log",
                newName: "IX_audit_log_changed_by");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                schema: "dcms",
                table: "audit_log",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "field_name",
                schema: "dcms",
                table: "audit_log",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "new_value",
                schema: "dcms",
                table: "audit_log",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "record_type",
                schema: "dcms",
                table: "audit_log",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_log",
                schema: "dcms",
                table: "audit_log",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_audit_log_users_changed_by",
                schema: "dcms",
                table: "audit_log",
                column: "changed_by",
                principalSchema: "dcms",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
