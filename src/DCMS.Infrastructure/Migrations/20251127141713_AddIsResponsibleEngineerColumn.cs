using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsResponsibleEngineerColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_responsible_engineer",
                schema: "dcms",
                table: "engineers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Update names to English and mark as responsible
            migrationBuilder.Sql(@"
                UPDATE dcms.engineers SET full_name = 'Eng. Azza', is_responsible_engineer = true WHERE full_name LIKE '%عزة الدسوقي%';
                UPDATE dcms.engineers SET full_name = 'Eng. Nada', is_responsible_engineer = true WHERE full_name LIKE '%ندي القصير%';
                UPDATE dcms.engineers SET full_name = 'Eng. Engy', is_responsible_engineer = true WHERE full_name LIKE '%انجي محمد%';
                UPDATE dcms.engineers SET full_name = 'Eng. Karam', is_responsible_engineer = true WHERE full_name LIKE '%احمد كرم%';
                UPDATE dcms.engineers SET full_name = 'Eng. Hadeer', is_responsible_engineer = true WHERE full_name LIKE '%هدير عمرو%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_responsible_engineer",
                schema: "dcms",
                table: "engineers");
        }
    }
}
