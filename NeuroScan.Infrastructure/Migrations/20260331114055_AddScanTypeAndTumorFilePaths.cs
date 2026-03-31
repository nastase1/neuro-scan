using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroScan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScanTypeAndTumorFilePaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScanType",
                table: "MriScans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StoredFilePathFlair",
                table: "MriScans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoredFilePathT1ce",
                table: "MriScans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoredFilePathT2",
                table: "MriScans",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScanType",
                table: "MriScans");

            migrationBuilder.DropColumn(
                name: "StoredFilePathFlair",
                table: "MriScans");

            migrationBuilder.DropColumn(
                name: "StoredFilePathT1ce",
                table: "MriScans");

            migrationBuilder.DropColumn(
                name: "StoredFilePathT2",
                table: "MriScans");
        }
    }
}
