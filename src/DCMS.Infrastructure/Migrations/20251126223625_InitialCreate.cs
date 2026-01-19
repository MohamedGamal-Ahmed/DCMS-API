using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dcms");

            migrationBuilder.CreateTable(
                name: "users",
                schema: "dcms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "dcms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    record_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    record_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    field_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    old_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    changed_by = table.Column<int>(type: "integer", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_log_users_changed_by",
                        column: x => x.changed_by,
                        principalSchema: "dcms",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "calendar_events",
                schema: "dcms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calendar_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_calendar_events_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "dcms",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "dcms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    signing_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responsible_engineer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    transferred_to = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "New"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    attachment_urls = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contracts", x => x.id);
                    table.ForeignKey(
                        name: "FK_contracts_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "dcms",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "emails",
                schema: "dcms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    from_email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    to_email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    subject = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    received_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responsible_engineer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "New"),
                    attachment_urls = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emails", x => x.id);
                    table.ForeignKey(
                        name: "FK_emails_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "dcms",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "inbound",
                schema: "dcms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subject_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    from_entity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    from_engineer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    subject = table.Column<string>(type: "text", nullable: false),
                    responsible_engineer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    inbound_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    transferred_to = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    transfer_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reply = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "New"),
                    attachment_urls = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbound", x => x.id);
                    table.ForeignKey(
                        name: "FK_inbound_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "dcms",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "dcms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    related_record_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Info"),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "dcms",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outbound",
                schema: "dcms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subject_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    to_entity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    to_engineer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    subject = table.Column<string>(type: "text", nullable: false),
                    related_inbound_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    responsible_engineer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    outbound_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attachment_urls = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbound", x => x.id);
                    table.ForeignKey(
                        name: "FK_outbound_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "dcms",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "event_attendees",
                schema: "dcms",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_attendees", x => new { x.event_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_event_attendees_calendar_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "dcms",
                        principalTable: "calendar_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_event_attendees_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "dcms",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_parties",
                schema: "dcms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    party_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    party_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    party_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_parties", x => x.id);
                    table.ForeignKey(
                        name: "FK_contract_parties_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "dcms",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_changed_by",
                schema: "dcms",
                table: "audit_log",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_created_by",
                schema: "dcms",
                table: "calendar_events",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_start_date",
                schema: "dcms",
                table: "calendar_events",
                column: "start_date");

            migrationBuilder.CreateIndex(
                name: "IX_contract_parties_contract_id",
                schema: "dcms",
                table: "contract_parties",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_created_by",
                schema: "dcms",
                table: "contracts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_project_name",
                schema: "dcms",
                table: "contracts",
                column: "project_name");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_signing_date",
                schema: "dcms",
                table: "contracts",
                column: "signing_date");

            migrationBuilder.CreateIndex(
                name: "IX_emails_created_by",
                schema: "dcms",
                table: "emails",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_event_attendees_user_id",
                schema: "dcms",
                table: "event_attendees",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_category",
                schema: "dcms",
                table: "inbound",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_created_by",
                schema: "dcms",
                table: "inbound",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_inbound_date",
                schema: "dcms",
                table: "inbound",
                column: "inbound_date");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_status",
                schema: "dcms",
                table: "inbound",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_subject_number",
                schema: "dcms",
                table: "inbound",
                column: "subject_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_is_read",
                schema: "dcms",
                table: "notifications",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id",
                schema: "dcms",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_created_by",
                schema: "dcms",
                table: "outbound",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_outbound_date",
                schema: "dcms",
                table: "outbound",
                column: "outbound_date");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_subject_number",
                schema: "dcms",
                table: "outbound",
                column: "subject_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                schema: "dcms",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                schema: "dcms",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "dcms");

            migrationBuilder.DropTable(
                name: "contract_parties",
                schema: "dcms");

            migrationBuilder.DropTable(
                name: "emails",
                schema: "dcms");

            migrationBuilder.DropTable(
                name: "event_attendees",
                schema: "dcms");

            migrationBuilder.DropTable(
                name: "inbound",
                schema: "dcms");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "dcms");

            migrationBuilder.DropTable(
                name: "outbound",
                schema: "dcms");

            migrationBuilder.DropTable(
                name: "contracts",
                schema: "dcms");

            migrationBuilder.DropTable(
                name: "calendar_events",
                schema: "dcms");

            migrationBuilder.DropTable(
                name: "users",
                schema: "dcms");
        }
    }
}
