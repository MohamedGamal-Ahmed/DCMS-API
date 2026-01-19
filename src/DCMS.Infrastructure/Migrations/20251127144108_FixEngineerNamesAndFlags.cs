using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixEngineerNamesAndFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        }
    }
}
