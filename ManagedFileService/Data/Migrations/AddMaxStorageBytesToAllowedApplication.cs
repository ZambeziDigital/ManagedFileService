using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagedFileService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxStorageBytesToAllowedApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MaxStorageBytes",
                table: "AllowedApplications",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxStorageBytes",
                table: "AllowedApplications");
        }
    }
}
