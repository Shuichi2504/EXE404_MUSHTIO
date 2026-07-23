using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTAgriculture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrphanTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[ChatMessages];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[SopCompletions];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[SopItems];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[SopSchedules];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[SopTemplates];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
