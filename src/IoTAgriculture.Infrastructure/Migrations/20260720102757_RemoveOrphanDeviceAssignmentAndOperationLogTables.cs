using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTAgriculture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrphanDeviceAssignmentAndOperationLogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[DeviceAssignmentRequests];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[DeviceAssignments];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[OperationLogs];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // These confirmed orphan tables are intentionally not recreated here.
            // Restore them from the DB backup if a data rollback is required.
        }
    }
}
